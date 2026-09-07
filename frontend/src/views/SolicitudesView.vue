<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-3xl font-bold text-[#003d7a]">Solicitudes</h1>
        <p class="text-gray-600 mt-1">Revisa y gestiona las propuestas de la Jefa Administrativa</p>
      </div>
    </div>

    <!-- Selector de origen: cambios a activos existentes vs. altas de activos nuevos -->
    <div class="flex gap-1 bg-white/90 backdrop-blur-sm rounded-xl p-1 border border-blue-100/50 shadow-sm w-fit">
      <button
        v-for="o in origenes"
        :key="o.value"
        @click="origen = o.value"
        :class="origen === o.value ? 'bg-[#003d7a] text-white shadow' : 'text-gray-600 hover:bg-gray-100'"
        class="px-5 py-2 rounded-lg text-sm font-medium transition flex items-center gap-2"
      >
        {{ o.label }}
        <span v-if="o.pendientes > 0"
          class="bg-red-500 text-white text-xs font-bold rounded-full w-5 h-5 flex items-center justify-center">
          {{ o.pendientes > 9 ? '9+' : o.pendientes }}
        </span>
      </button>
    </div>

    <!-- Descripción del origen seleccionado -->
    <p class="text-sm text-gray-500 -mt-3 max-w-3xl leading-relaxed">{{ origenDescripcion }}</p>

    <!-- Tabs de estado -->
    <div class="flex gap-1 bg-white/90 backdrop-blur-sm rounded-xl p-1 border border-blue-100/50 shadow-sm w-fit">
      <button
        v-for="tab in tabs"
        :key="tab.value"
        @click="estadoFiltro = tab.value"
        :class="estadoFiltro === tab.value
          ? 'bg-[#003d7a] text-white shadow'
          : 'text-gray-600 hover:bg-gray-100'"
        class="px-5 py-2 rounded-lg text-sm font-medium transition flex items-center gap-2"
      >
        {{ tab.label }}
        <span v-if="tab.value === 'Pendiente' && pendientes > 0"
          class="bg-red-500 text-white text-xs font-bold rounded-full w-5 h-5 flex items-center justify-center">
          {{ pendientes > 9 ? '9+' : pendientes }}
        </span>
      </button>
    </div>

    <!-- ════════ ALTAS DE ACTIVOS NUEVOS ════════ -->
    <template v-if="origen === 'altas'">
      <div v-if="cargandoAltas" class="text-center py-16 text-gray-400">Cargando...</div>
      <div v-else-if="altas.length === 0" class="bg-white/90 rounded-2xl shadow-lg border border-blue-100/50 px-8 py-16 text-center text-gray-400">
        No hay solicitudes de inscripción {{ estadoFiltro === 'Pendiente' ? 'pendientes' : estadoFiltro === 'Aprobada' ? 'aprobadas' : 'rechazadas' }}.
      </div>
      <div v-else class="space-y-4">
        <div v-for="a in altas" :key="a.id" class="bg-white rounded-2xl shadow-md border border-blue-200 overflow-hidden">
          <div class="flex items-center justify-between px-6 py-4 border-b border-gray-200 bg-blue-50/60">
            <div class="flex items-center gap-3">
              <span class="font-bold text-[#003d7a] font-mono text-lg">{{ a.placa }}</span>
              <span class="mx-1 text-gray-300">·</span>
              <span class="text-gray-700 font-medium">{{ a.articulo }}</span>
              <span :class="estadoBadge(a.estado)" class="px-3 py-0.5 rounded-full text-xs font-semibold">{{ a.estado }}</span>
              <span class="flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-blue-100 text-blue-700">Inscripción</span>
            </div>
            <div class="text-right text-sm text-gray-500">
              <p>Solicitado por <span class="font-medium text-gray-700">{{ a.solicitanteNombre }}</span></p>
              <p class="text-xs mt-0.5">{{ formatFecha(a.fechaSolicitud) }}</p>
            </div>
          </div>

          <div class="p-6 grid grid-cols-2 md:grid-cols-3 gap-x-6 gap-y-2">
            <div v-for="f in camposAlta(a)" :key="f.label" class="flex items-baseline gap-2">
              <span class="text-xs text-gray-400 w-24 flex-shrink-0">{{ f.label }}</span>
              <span class="text-sm font-medium text-gray-700 truncate">{{ f.valor || '—' }}</span>
            </div>
          </div>

          <div v-if="a.estado !== 'Pendiente'" class="px-6 py-3 border-t border-gray-100 flex items-center gap-3"
            :class="a.estado === 'Aprobada' ? 'bg-green-50/60' : 'bg-red-50/60'">
            <p class="text-sm" :class="a.estado === 'Aprobada' ? 'text-green-700' : 'text-red-600'">
              <span class="font-medium">{{ a.estado === 'Aprobada' ? 'Aprobado' : 'Rechazado' }}</span>
              por {{ a.revisorNombre }} · {{ formatFecha(a.fechaResolucion) }}
              <span v-if="a.comentario" class="italic ml-1">"{{ a.comentario }}"</span>
            </p>
          </div>

          <div v-if="a.estado === 'Pendiente'" class="px-6 py-4 border-t border-gray-100 bg-gray-50/50 flex justify-end">
            <button @click="altaSeleccionada = a"
              class="px-5 py-2 bg-[#003d7a] text-white rounded-lg hover:bg-[#002d5a] transition font-medium text-sm">
              Revisar
            </button>
          </div>
        </div>
      </div>

      <SolicitudAltaModal
        v-if="altaSeleccionada"
        :solicitud="altaSeleccionada"
        :categorias="categorias"
        :encargados="encargados"
        @close="altaSeleccionada = null"
        @resolved="onAltaResuelta"
      />
    </template>

    <!-- ════════ CAMBIOS A ACTIVOS EXISTENTES ════════ -->
    <template v-else>
    <!-- Lista de solicitudes -->
    <div v-if="cargando" class="text-center py-16 text-gray-400">Cargando...</div>

    <div v-else-if="solicitudes.length === 0" class="bg-white/90 rounded-2xl shadow-lg border border-blue-100/50 px-8 py-16 text-center text-gray-400">
      No hay solicitudes {{ estadoFiltro === 'Pendiente' ? 'pendientes' : estadoFiltro === 'Aprobada' ? 'aprobadas' : 'rechazadas' }}.
    </div>

    <div v-else class="space-y-4">
      <div
        v-for="s in solicitudes"
        :key="s.id"
        class="bg-white rounded-2xl shadow-md border overflow-hidden"
        :class="s.datosNuevos.estado === 'Desecho' ? 'border-red-300' : 'border-blue-200'"
      >
        <!-- Cabecera de la tarjeta -->
        <div class="flex items-center justify-between px-6 py-4 border-b border-gray-200"
          :class="s.datosNuevos.estado === 'Desecho' ? 'bg-red-50/70' : 'bg-blue-50/60'">
          <div class="flex items-center gap-3">
            <div>
              <span class="font-bold text-[#003d7a] font-mono text-lg">{{ s.activoPlaca }}</span>
              <span class="mx-2 text-gray-300">·</span>
              <span class="text-gray-700 font-medium">{{ s.articuloActual }}</span>
            </div>
            <span :class="estadoBadge(s.estado)" class="px-3 py-0.5 rounded-full text-xs font-semibold">
              {{ s.estado }}
            </span>
            <span v-if="s.datosNuevos.estado === 'Desecho'"
              class="flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-red-100 text-red-700">
              <svg xmlns="http://www.w3.org/2000/svg" class="w-3 h-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
              </svg>
              Desecho
            </span>
          </div>
          <div class="text-right text-sm text-gray-500">
            <p>Solicitado por <span class="font-medium text-gray-700">{{ s.solicitanteNombre }}</span></p>
            <p class="text-xs mt-0.5">{{ formatFecha(s.fechaSolicitud) }}</p>
          </div>
        </div>

        <!-- Contenido -->
        <div class="p-6">
          <!-- Solicitud de desecho: vista simplificada -->
          <template v-if="s.datosNuevos.estado === 'Desecho'">
            <div class="flex gap-6">
              <div class="flex-1">
                <p class="text-xs text-gray-400 uppercase tracking-wide font-medium mb-3">Activo a desechar</p>
                <div class="space-y-2">
                  <div v-for="f in camposActuales(s)" :key="f.label" class="flex items-baseline gap-2">
                    <span class="text-xs text-gray-400 w-20 flex-shrink-0">{{ f.label }}</span>
                    <span class="text-sm font-medium text-gray-700">{{ f.valor || '—' }}</span>
                  </div>
                </div>
              </div>
              <div class="flex items-center justify-center px-8 border-l border-red-100">
                <div class="text-center">
                  <svg xmlns="http://www.w3.org/2000/svg" class="w-10 h-10 text-red-300 mx-auto mb-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                  </svg>
                  <p class="text-xs font-semibold text-red-500 uppercase tracking-wide">Envío a desecho</p>
                  <p class="text-xs text-gray-400 mt-1">Sin cambios adicionales</p>
                </div>
              </div>
            </div>
          </template>

          <!-- Solicitud de cambios: vista comparativa normal -->
          <template v-else>
            <div class="grid grid-cols-2 gap-6">
              <div>
                <p class="text-xs text-gray-400 uppercase tracking-wide font-medium mb-3">Estado actual</p>
                <div class="space-y-2">
                  <div v-for="f in camposActuales(s)" :key="f.label" class="flex items-baseline gap-2">
                    <span class="text-xs text-gray-400 w-20 flex-shrink-0">{{ f.label }}</span>
                    <span class="text-sm font-medium text-gray-700 flex-1 truncate">{{ f.valor || '—' }}</span>
                  </div>
                </div>
              </div>
              <div>
                <p class="text-xs text-amber-500 uppercase tracking-wide font-medium mb-3">Cambios propuestos</p>
                <div class="space-y-2">
                  <div v-for="f in camposPropuestos(s)" :key="f.label" class="flex items-baseline gap-2">
                    <span class="text-xs text-gray-400 w-20 flex-shrink-0">{{ f.label }}</span>
                    <span class="text-sm font-medium flex-1 truncate"
                      :class="f.cambia ? 'text-amber-700 bg-amber-50 px-1 rounded' : 'text-gray-700'">
                      {{ f.valor || '—' }}
                    </span>
                  </div>
                </div>
              </div>
            </div>
            <div class="mt-4 pt-4 border-t border-gray-200 grid grid-cols-3 gap-3">
              <div class="flex items-baseline gap-2">
                <span class="text-xs text-gray-400 w-20 flex-shrink-0">N° Serial</span>
                <span class="text-sm text-gray-700">{{ s.datosNuevos.numSerial || '—' }}</span>
              </div>
              <div class="flex items-baseline gap-2">
                <span class="text-xs text-gray-400 w-20 flex-shrink-0">Categoría</span>
                <span class="text-sm text-gray-700">{{ s.datosNuevos.categoriaNombre || '—' }}</span>
              </div>
              <div v-if="s.datosNuevos.observaciones" class="flex items-baseline gap-2">
                <span class="text-xs text-gray-400 w-24 flex-shrink-0">Observaciones</span>
                <span class="text-sm text-gray-700 italic">{{ s.datosNuevos.observaciones }}</span>
              </div>
            </div>
          </template>

          <!-- Resolución (aprobada/rechazada) -->
          <div v-if="s.estado !== 'Pendiente'" class="mt-4 pt-4 border-t border-gray-100 flex items-start gap-3">
            <div class="flex-1 text-sm text-gray-600">
              <span class="font-medium">{{ s.estado === 'Aprobada' ? 'Aprobado' : 'Rechazado' }} por</span>
              {{ s.revisorNombre }} · {{ formatFecha(s.fechaResolucion) }}
            </div>
            <div v-if="s.comentario" class="flex-1 text-sm text-gray-600 italic">
              "{{ s.comentario }}"
            </div>
          </div>
        </div>

        <!-- Footer de acciones -->
        <div v-if="s.estado === 'Pendiente'" class="px-6 py-4 border-t border-gray-100 bg-gray-50/50">
          <!-- Formulario inline de rechazo -->
          <div v-if="rechazandoId === s.id" class="space-y-3">
            <textarea
              v-model="comentarioRechazo"
              rows="2"
              placeholder="Motivo del rechazo (opcional)"
              class="w-full px-3 py-2 text-sm border border-red-200 rounded-lg focus:ring-2 focus:ring-red-400/30 focus:border-red-400 outline-none resize-none"
            />
            <div class="flex gap-3">
              <button @click="rechazandoId = null; comentarioRechazo = ''"
                class="px-5 py-2 border border-gray-300 rounded-lg hover:bg-gray-100 transition font-medium text-sm">
                Cancelar
              </button>
              <button @click="confirmarRechazo(s)"
                :disabled="procesando === s.id"
                class="px-5 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition font-medium text-sm disabled:opacity-50 flex items-center gap-2">
                <svg v-if="procesando === s.id" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"/>
                </svg>
                Confirmar rechazo
              </button>
            </div>
          </div>

          <!-- Botones principales -->
          <div v-else class="flex gap-3">
            <button
              @click="rechazandoId = s.id"
              :disabled="procesando === s.id"
              class="px-5 py-2 border border-red-200 text-red-600 rounded-lg hover:bg-red-50 transition font-medium text-sm disabled:opacity-50"
            >
              Rechazar
            </button>
            <button
              @click="aprobarSolicitud(s)"
              :disabled="procesando === s.id"
              class="px-5 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition font-medium text-sm disabled:opacity-50 flex items-center gap-2"
            >
              <svg v-if="procesando === s.id" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"/>
              </svg>
              Aprobar
            </button>
          </div>
        </div>
      </div>
    </div>
    </template>
  </div>
</template>

<script setup>
import { ref, watch, onMounted, computed } from 'vue'
import { useDialog } from '@/composables/useDialog'
import solicitudService from '@/services/solicitudService'
import solicitudActivoService from '@/services/solicitudActivoService'
import categoriaService from '@/services/categoriaService'
import encargadoService from '@/services/encargadoService'
import SolicitudAltaModal from '@/components/SolicitudAltaModal.vue'

function camposActuales(s) {
  return [
    { label: 'Artículo',  valor: s.articuloActual },
    { label: 'Marca',     valor: s.marcaActual },
    { label: 'Modelo',    valor: s.modeloActual },
    { label: 'Ubicación', valor: s.ubicacionActual },
    { label: 'Encargado', valor: s.encargadoActual },
  ]
}

function camposPropuestos(s) {
  const d = s.datosNuevos
  return [
    { label: 'Artículo',  valor: d.articulo,        cambia: d.articulo !== s.articuloActual },
    { label: 'Marca',     valor: d.marca,            cambia: d.marca !== s.marcaActual },
    { label: 'Modelo',    valor: d.modelo,           cambia: d.modelo !== s.modeloActual },
    { label: 'Ubicación', valor: d.ubicacionActual,  cambia: d.ubicacionActual !== s.ubicacionActual },
    { label: 'Encargado', valor: d.encargadoNombre,  cambia: d.encargadoNombre !== s.encargadoActual },
  ]
}

const dialog = useDialog()

const tabs = [
  { label: 'Pendientes', value: 'Pendiente' },
  { label: 'Aprobadas', value: 'Aprobada' },
  { label: 'Rechazadas', value: 'Rechazada' }
]

const origen = ref('altas') // 'altas' | 'cambios'
const estadoFiltro = ref('Pendiente')

// ── Cambios a activos existentes ──
const solicitudes = ref([])
const cargando = ref(false)
const procesando = ref(null)
const rechazandoId = ref(null)
const comentarioRechazo = ref('')

// ── Altas de activos nuevos ──
const altas = ref([])
const cargandoAltas = ref(false)
const altaSeleccionada = ref(null)
const categorias = ref([])
const encargados = ref([])

// Conteos globales de pendientes para los badges del selector de origen
const cambiosPendientes = ref(0)
const altasPendientes = ref(0)

const pendientes = computed(() => {
  if (estadoFiltro.value !== 'Pendiente') return 0
  return origen.value === 'altas' ? altas.value.length : solicitudes.value.length
})

const origenes = computed(() => [
  { label: 'Inscripción de activos', value: 'altas', pendientes: altasPendientes.value },
  { label: 'Cambios a activos', value: 'cambios', pendientes: cambiosPendientes.value }
])

const origenDescripcion = computed(() =>
  origen.value === 'altas'
    ? 'Inscripción de activos: altas de activos nuevos, uno a uno, propuestas por la Jefa Administrativa. Al aprobar, el activo queda registrado oficialmente en el inventario con su placa, categoría y encargado (si propuso una categoría o un encargado nuevos, se crean en ese momento).'
    : 'Cambios a activos: modificaciones sobre un activo ya registrado (artículo, marca, modelo, ubicación o encargado) o su envío a desecho. Al aprobar, los cambios se aplican de inmediato al activo existente.'
)

async function cargar() {
  cargando.value = true
  try {
    const { data } = await solicitudService.listar(estadoFiltro.value)
    solicitudes.value = data
  } finally {
    cargando.value = false
  }
}

async function cargarAltas() {
  cargandoAltas.value = true
  try {
    const { data } = await solicitudActivoService.listar(estadoFiltro.value)
    altas.value = data
  } finally {
    cargandoAltas.value = false
  }
}

async function refrescarConteos() {
  try {
    const [c, a] = await Promise.all([
      solicitudService.contarPendientes(),
      solicitudActivoService.contarPendientes()
    ])
    cambiosPendientes.value = c.data.count
    altasPendientes.value = a.data.count
  } catch { /* badges son informativos */ }
}

function cargarActual() {
  return origen.value === 'altas' ? cargarAltas() : cargar()
}

onMounted(async () => {
  const [catRes, encRes] = await Promise.all([categoriaService.listar(), encargadoService.listar()])
  categorias.value = catRes.data
  encargados.value = encRes.data
  await Promise.all([cargarActual(), refrescarConteos()])
})

watch([estadoFiltro, origen], () => {
  rechazandoId.value = null
  comentarioRechazo.value = ''
  cargarActual()
})

function camposAlta(a) {
  return [
    { label: 'Tipo placa',  valor: a.tipoPlaca },
    { label: 'Marca',       valor: a.marca },
    { label: 'Modelo',      valor: a.modelo },
    { label: 'N° Serial',   valor: a.numSerial },
    { label: 'Categoría',   valor: a.categoriaNuevaNombre ? `${a.categoriaNuevaNombre} · nueva` : a.categoriaNombre },
    { label: 'Ubicación',   valor: a.ubicacionActual },
    { label: 'Encargado',   valor: a.encargadoNuevoNombre ? `${a.encargadoNuevoNombre} — ${a.encargadoNuevoRol} · nuevo` : a.encargadoNombre },
    { label: 'Observaciones', valor: a.observaciones }
  ]
}

async function onAltaResuelta() {
  altaSeleccionada.value = null
  await Promise.all([cargarAltas(), refrescarConteos()])
}

async function aprobarSolicitud(s) {
  const ok = await dialog.confirm({
    title: 'Aprobar solicitud',
    message: `¿Confirmas la aprobación de los cambios propuestos para el activo <strong>${s.activoPlaca}</strong>? Los cambios se aplicarán inmediatamente.`,
    confirmText: 'Aprobar',
    type: 'info'
  })
  if (!ok) return
  procesando.value = s.id
  try {
    await solicitudService.aprobar(s.id)
    await Promise.all([cargar(), refrescarConteos()])
  } catch (e) {
    await dialog.alert({
      title: 'Error',
      message: e.response?.data?.mensaje || 'No se pudo aprobar la solicitud.',
      type: 'danger'
    })
  } finally {
    procesando.value = null
  }
}

async function confirmarRechazo(s) {
  procesando.value = s.id
  try {
    await solicitudService.rechazar(s.id, comentarioRechazo.value || null)
    rechazandoId.value = null
    comentarioRechazo.value = ''
    await Promise.all([cargar(), refrescarConteos()])
  } catch (e) {
    await dialog.alert({
      title: 'Error',
      message: e.response?.data?.mensaje || 'No se pudo rechazar la solicitud.',
      type: 'danger'
    })
  } finally {
    procesando.value = null
  }
}

function formatFecha(iso) {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString('es-CR', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

const estadoBadges = {
  Pendiente: 'bg-amber-100 text-amber-700',
  Aprobada: 'bg-green-100 text-green-700',
  Rechazada: 'bg-red-100 text-red-700'
}
function estadoBadge(e) { return estadoBadges[e] || 'bg-gray-100 text-gray-600' }
</script>

