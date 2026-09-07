<template>
  <div class="space-y-6">
    <div>
      <h1 class="text-3xl font-bold text-[#003d7a]">Mis Solicitudes</h1>
      <p class="text-gray-600 mt-1">Seguimiento de lo que enviaste a Administradora o GTI</p>
    </div>

    <!-- Selector de origen -->
    <div class="flex gap-1 bg-white rounded-xl p-1 border border-gray-200 shadow-sm w-fit">
      <button
        v-for="o in origenes"
        :key="o.value"
        @click="origen = o.value"
        :class="origen === o.value ? 'bg-[#003d7a] text-white shadow' : 'text-gray-600 hover:bg-gray-100'"
        class="px-5 py-2 rounded-lg text-sm font-medium transition"
      >
        {{ o.label }}
      </button>
    </div>

    <!-- Descripción del origen seleccionado -->
    <p class="text-sm text-gray-500 -mt-3 max-w-3xl leading-relaxed">{{ origenDescripcion }}</p>

    <!-- Tabs -->
    <div class="flex gap-1 bg-white rounded-xl p-1 border border-gray-200 shadow-sm w-fit">
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
        <span v-if="tab.value === 'Pendiente' && pendienteCount > 0"
          class="bg-amber-400 text-white text-xs font-bold rounded-full w-5 h-5 flex items-center justify-center">
          {{ pendienteCount > 9 ? '9+' : pendienteCount }}
        </span>
      </button>
    </div>

    <!-- ════════ ALTAS DE ACTIVOS NUEVOS ════════ -->
    <template v-if="origen === 'altas'">
      <div v-if="cargandoAltas" class="text-center py-16 text-gray-400">Cargando...</div>
      <div v-else-if="altas.length === 0"
        class="bg-white rounded-2xl shadow-lg border border-gray-200 px-8 py-16 text-center text-gray-400">
        No tienes solicitudes de inscripción {{ tab(estadoFiltro) }}.
      </div>
      <div v-else class="space-y-4">
        <div v-for="a in altas" :key="a.id"
          class="bg-white rounded-2xl shadow-md border overflow-hidden" :class="estadoBorde(a.estado)">
          <div class="flex items-center justify-between px-6 py-4 border-b border-gray-200 bg-gray-50/60">
            <div class="flex items-center gap-3">
              <span class="font-bold text-[#003d7a] font-mono">{{ a.placa }}</span>
              <span class="mx-1 text-gray-300">·</span>
              <span class="text-gray-700 font-medium">{{ a.articulo }}</span>
              <span :class="estadoBadge(a.estado)" class="px-3 py-0.5 rounded-full text-xs font-semibold">{{ a.estado }}</span>
              <span class="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-blue-100 text-blue-700">Inscripción</span>
            </div>
            <span class="text-xs text-gray-400">{{ formatFecha(a.fechaSolicitud) }}</span>
          </div>

          <div class="px-6 py-4 grid grid-cols-2 md:grid-cols-3 gap-x-6 gap-y-2">
            <div v-for="f in camposAlta(a)" :key="f.label" class="flex items-baseline gap-2">
              <span class="text-xs text-gray-400 w-24 flex-shrink-0">{{ f.label }}</span>
              <span class="text-sm font-medium text-gray-700 truncate">{{ f.valor || '—' }}</span>
            </div>
          </div>

          <div v-if="a.estado !== 'Pendiente'"
            class="px-6 py-3 border-t border-gray-100 flex items-center gap-3"
            :class="a.estado === 'Aprobada' ? 'bg-green-50/60' : 'bg-red-50/60'">
            <p class="text-sm" :class="a.estado === 'Aprobada' ? 'text-green-700' : 'text-red-600'">
              <span class="font-medium">{{ a.estado === 'Aprobada' ? 'Aprobado' : 'Rechazado' }}</span>
              por {{ a.revisorNombre }} · {{ formatFecha(a.fechaResolucion) }}
              <span v-if="a.comentario" class="italic ml-1">"{{ a.comentario }}"</span>
              <span v-if="a.estado === 'Aprobada' && a.placaCreada" class="ml-1">· Registrado como {{ a.placaCreada }}</span>
            </p>
          </div>
          <div v-else class="px-6 py-3 border-t border-amber-100 bg-amber-50/40 flex items-center gap-2">
            <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4 text-amber-500 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <p class="text-sm italic text-amber-700">En espera de aprobación por Administradora o GTI</p>
          </div>
        </div>
      </div>
    </template>

    <!-- ════════ CAMBIOS A ACTIVOS EXISTENTES ════════ -->
    <template v-else>
    <!-- Estado vacío -->
    <div v-if="cargando" class="text-center py-16 text-gray-400">Cargando...</div>
    <div v-else-if="solicitudes.length === 0"
      class="bg-white rounded-2xl shadow-lg border border-gray-200 px-8 py-16 text-center">
      <svg xmlns="http://www.w3.org/2000/svg" class="w-12 h-12 text-gray-300 mx-auto mb-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
      </svg>
      <p class="text-gray-400">No tienes solicitudes {{ tab(estadoFiltro) }}.</p>
      <button v-if="estadoFiltro !== 'Pendiente'" @click="estadoFiltro = 'Pendiente'"
        class="mt-3 text-sm text-[#0066cc] hover:underline">Ver pendientes</button>
    </div>

    <!-- Lista -->
    <div v-else class="space-y-4">
      <div v-for="s in solicitudes" :key="s.id"
        class="bg-white rounded-2xl shadow-md border overflow-hidden"
        :class="estadoBorde(s.estado)">

        <!-- Cabecera -->
        <div class="flex items-center justify-between px-6 py-4 border-b border-gray-200"
          :class="s.datosNuevos.estado === 'Desecho' ? 'bg-red-50/60' : 'bg-gray-50/60'">
          <div class="flex items-center gap-3">
            <div>
              <span class="font-bold text-[#003d7a] font-mono">{{ s.activoPlaca }}</span>
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
          <span class="text-xs text-gray-400">{{ formatFecha(s.fechaSolicitud) }}</span>
        </div>

        <!-- Cambios propuestos -->
        <div class="px-6 py-4">
          <template v-if="s.datosNuevos.estado === 'Desecho'">
            <div class="flex items-center gap-3 text-sm text-red-700">
              <svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5 text-red-400 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
              </svg>
              Solicitud de envío a desecho — no se proponen cambios adicionales al activo.
            </div>
          </template>
          <template v-else>
            <p class="text-xs text-gray-400 uppercase tracking-wide font-medium mb-3">Cambios propuestos</p>
            <div class="grid grid-cols-2 gap-x-8 gap-y-1.5">
              <template v-for="campo in calcularCambios(s)" :key="campo.label">
                <div class="flex items-baseline gap-2 col-span-1">
                  <span class="text-xs text-gray-400 w-20 flex-shrink-0">{{ campo.label }}</span>
                  <span v-if="campo.cambia" class="text-sm">
                    <span class="line-through text-gray-400">{{ campo.actual }}</span>
                    <span class="italic font-medium text-amber-700 ml-1">→ {{ campo.propuesto }}</span>
                  </span>
                  <span v-else class="text-sm text-gray-500 italic">sin cambio</span>
                </div>
              </template>
            </div>
          </template>
        </div>

        <!-- Resolución -->
        <div v-if="s.estado !== 'Pendiente'"
          class="px-6 py-3 border-t border-gray-100 flex items-center gap-3"
          :class="s.estado === 'Aprobada' ? 'bg-green-50/60' : 'bg-red-50/60'">
          <svg v-if="s.estado === 'Aprobada'" xmlns="http://www.w3.org/2000/svg" class="w-4 h-4 text-green-600 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
          </svg>
          <svg v-else xmlns="http://www.w3.org/2000/svg" class="w-4 h-4 text-red-500 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
          <p class="text-sm" :class="s.estado === 'Aprobada' ? 'text-green-700' : 'text-red-600'">
            <span class="font-medium">{{ s.estado === 'Aprobada' ? 'Aprobado' : 'Rechazado' }}</span>
            por {{ s.revisorNombre }} · {{ formatFecha(s.fechaResolucion) }}
            <span v-if="s.comentario" class="italic ml-1">"{{ s.comentario }}"</span>
          </p>
        </div>

        <!-- Pendiente -->
        <div v-else class="px-6 py-3 border-t flex items-center gap-2"
          :class="s.datosNuevos.estado === 'Desecho'
            ? 'border-red-100 bg-red-50/40'
            : 'border-amber-100 bg-amber-50/40'">
          <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4 flex-shrink-0"
            :class="s.datosNuevos.estado === 'Desecho' ? 'text-red-400' : 'text-amber-500'"
            fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <p class="text-sm italic"
            :class="s.datosNuevos.estado === 'Desecho' ? 'text-red-600' : 'text-amber-700'">
            En espera de revisión por Administradora o GTI
          </p>
        </div>
      </div>
    </div>
    </template>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import solicitudService from '@/services/solicitudService'
import solicitudActivoService from '@/services/solicitudActivoService'

const tabs = [
  { label: 'Pendientes', value: 'Pendiente' },
  { label: 'Aprobadas',  value: 'Aprobada' },
  { label: 'Rechazadas', value: 'Rechazada' }
]

const origenes = [
  { label: 'Inscripción de activos', value: 'altas' },
  { label: 'Cambios a activos', value: 'cambios' }
]

const origen = ref('altas')
const estadoFiltro = ref('Pendiente')

const origenDescripcion = computed(() =>
  origen.value === 'altas'
    ? 'Inscripción de activos: activos nuevos que enviaste para registrar en el inventario, uno a uno. Quedan oficiales cuando Administradora o GTI los aprueban; ahí también se crean la categoría o el encargado que hayas propuesto.'
    : 'Cambios a activos: modificaciones que propusiste sobre un activo ya registrado (artículo, marca, modelo, ubicación o encargado) o su envío a desecho. Se aplican cuando Administradora o GTI las aprueban.'
)
const solicitudes = ref([])
const cargando = ref(false)
const altas = ref([])
const cargandoAltas = ref(false)

const pendienteCount = computed(() => {
  if (estadoFiltro.value !== 'Pendiente') return 0
  return origen.value === 'altas' ? altas.value.length : solicitudes.value.length
})

async function cargar() {
  cargando.value = true
  try {
    const { data } = await solicitudService.listarMias(estadoFiltro.value)
    solicitudes.value = data
  } finally {
    cargando.value = false
  }
}

async function cargarAltas() {
  cargandoAltas.value = true
  try {
    const { data } = await solicitudActivoService.listarMias(estadoFiltro.value)
    altas.value = data
  } finally {
    cargandoAltas.value = false
  }
}

function cargarActual() {
  return origen.value === 'altas' ? cargarAltas() : cargar()
}

function camposAlta(a) {
  return [
    { label: 'Tipo placa',    valor: a.tipoPlaca },
    { label: 'Marca',         valor: a.marca },
    { label: 'Modelo',        valor: a.modelo },
    { label: 'N° Serial',     valor: a.numSerial },
    { label: 'Categoría',     valor: a.categoriaNuevaNombre ? `${a.categoriaNuevaNombre} · nueva` : a.categoriaNombre },
    { label: 'Ubicación',     valor: a.ubicacionActual },
    { label: 'Encargado',     valor: a.encargadoNuevoNombre ? `${a.encargadoNuevoNombre} — ${a.encargadoNuevoRol} · nuevo` : a.encargadoNombre },
    { label: 'Observaciones', valor: a.observaciones }
  ]
}

onMounted(cargarActual)
watch([estadoFiltro, origen], cargarActual)

function calcularCambios(s) {
  const d = s.datosNuevos
  const a = s
  return [
    { label: 'Artículo',  actual: a.articuloActual,  propuesto: d.articulo,        cambia: d.articulo !== a.articuloActual },
    { label: 'Marca',     actual: a.marcaActual,     propuesto: d.marca,           cambia: d.marca !== a.marcaActual },
    { label: 'Modelo',    actual: a.modeloActual,    propuesto: d.modelo,          cambia: d.modelo !== a.modeloActual },
    { label: 'Ubicación', actual: a.ubicacionActual, propuesto: d.ubicacionActual, cambia: d.ubicacionActual !== a.ubicacionActual },
    { label: 'Encargado', actual: a.encargadoActual, propuesto: d.encargadoNombre, cambia: d.encargadoNombre !== a.encargadoActual },
  ].filter(c => c.cambia || true)
}

function tab(v) {
  return { Pendiente: 'pendientes', Aprobada: 'aprobadas', Rechazada: 'rechazadas' }[v] || ''
}

function formatFecha(iso) {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString('es-CR', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

const estadoBadges = {
  Pendiente: 'bg-amber-100 text-amber-700',
  Aprobada:  'bg-green-100 text-green-700',
  Rechazada: 'bg-red-100 text-red-700'
}
const estadoBordes = {
  Pendiente: 'border-amber-300',
  Aprobada:  'border-green-300',
  Rechazada: 'border-red-300'
}
function estadoBadge(e) { return estadoBadges[e] || 'bg-gray-100 text-gray-600' }
function estadoBorde(e) { return estadoBordes[e] || 'border-blue-100/50' }
</script>
