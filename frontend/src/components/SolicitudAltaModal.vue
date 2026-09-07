<template>
  <Teleport to="body">
  <div class="fixed inset-0 bg-black/50 flex items-center justify-center p-4" style="z-index: 9999" @click.self="$emit('close')">
    <div class="bg-white rounded-2xl shadow-2xl w-full max-w-2xl max-h-[90vh] flex flex-col">

      <!-- Header -->
      <div class="bg-[#003d7a] text-white px-6 py-4 flex items-center justify-between rounded-t-2xl flex-shrink-0">
        <div>
          <h2 class="text-xl font-bold">Revisar Inscripción de Activo</h2>
          <p class="text-blue-200 text-sm mt-0.5">
            Solicitada por {{ solicitud.solicitanteNombre }} · {{ formatFecha(solicitud.fechaSolicitud) }}
          </p>
        </div>
        <button @click="$emit('close')" class="p-1 hover:bg-white/10 rounded transition">
          <svg xmlns="http://www.w3.org/2000/svg" class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>

      <div class="overflow-y-auto flex-1 p-6 space-y-6">
        <p class="text-xs text-gray-500 bg-blue-50 border border-blue-100 rounded-lg px-3 py-2">
          Puedes corregir cualquier dato antes de aprobar. Al aprobar, el activo queda registrado oficialmente
          en el inventario. Al rechazar, no se crea nada y la Jefa Administrativa verá el motivo.
        </p>

        <!-- Aviso: la inscripción propone crear catálogo nuevo -->
        <div v-if="proponeCategoria || proponeEncargado"
          class="text-xs text-amber-800 bg-amber-50 border border-amber-200 rounded-lg px-3 py-2">
          <span class="font-semibold">Esta inscripción propone crear:</span>
          <ul class="mt-0.5 list-disc list-inside">
            <li v-if="proponeCategoria">Categoría «{{ solicitud.categoriaNuevaNombre }}»{{ solicitud.categoriaNuevaIcono ? ` ${solicitud.categoriaNuevaIcono}` : '' }}</li>
            <li v-if="proponeEncargado">Encargado «{{ solicitud.encargadoNuevoNombre }}» — {{ solicitud.encargadoNuevoRol }}</li>
          </ul>
          <p class="mt-0.5">Se crean solo al aprobar. Puedes editar la propuesta o cambiarla por una existente.</p>
        </div>

        <!-- Identificación -->
        <div>
          <p class="text-xs text-gray-400 uppercase tracking-wide font-medium mb-3">Identificación</p>
          <div class="bg-gray-50/70 rounded-xl p-4 space-y-3">
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="block text-xs font-medium text-gray-500 mb-1">Placa <span class="text-red-500">*</span></label>
                <input v-model="form.placa" type="text" maxlength="10"
                  class="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:ring-2 focus:ring-[#0066cc] focus:border-transparent outline-none bg-white font-mono" />
              </div>
              <div>
                <label class="block text-xs font-medium text-gray-500 mb-1">Tipo de Placa <span class="text-red-500">*</span></label>
                <select v-model="form.tipoPlaca"
                  class="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:ring-2 focus:ring-[#0066cc] focus:border-transparent outline-none bg-white">
                  <option value="Institucional">Institucional</option>
                  <option value="Interno">Interno</option>
                </select>
              </div>
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="block text-xs font-medium text-gray-500 mb-1">Artículo <span class="text-red-500">*</span></label>
                <input v-model="form.articulo" type="text" maxlength="20"
                  class="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:ring-2 focus:ring-[#0066cc] focus:border-transparent outline-none bg-white" />
              </div>
              <div>
                <label class="block text-xs font-medium text-gray-500 mb-1">Número Serial <span class="text-red-500">*</span></label>
                <input v-model="form.numSerial" type="text" maxlength="30"
                  class="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:ring-2 focus:ring-[#0066cc] focus:border-transparent outline-none bg-white font-mono" />
              </div>
            </div>
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="block text-xs font-medium text-gray-500 mb-1">Marca <span class="text-red-500">*</span></label>
                <input v-model="form.marca" type="text" maxlength="30"
                  class="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:ring-2 focus:ring-[#0066cc] focus:border-transparent outline-none bg-white" />
              </div>
              <div>
                <label class="block text-xs font-medium text-gray-500 mb-1">Modelo <span class="text-red-500">*</span></label>
                <input v-model="form.modelo" type="text" maxlength="20"
                  class="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:ring-2 focus:ring-[#0066cc] focus:border-transparent outline-none bg-white" />
              </div>
            </div>
          </div>
        </div>

        <!-- Clasificación -->
        <div>
          <p class="text-xs text-gray-400 uppercase tracking-wide font-medium mb-3">Clasificación</p>
          <div class="bg-gray-50/70 rounded-xl p-4">
            <label class="block text-xs font-medium text-gray-500 mb-1">Categoría <span class="text-red-500">*</span></label>
            <div class="flex gap-1.5 mb-2">
              <button type="button" @click="form.catModo = 'existente'"
                class="px-2.5 py-1 rounded-md text-xs font-medium transition"
                :class="form.catModo === 'existente' ? 'bg-[#003d7a] text-white' : 'bg-white border border-gray-200 text-gray-500'">
                Existente
              </button>
              <button type="button" @click="form.catModo = 'nueva'"
                class="px-2.5 py-1 rounded-md text-xs font-medium transition"
                :class="form.catModo === 'nueva' ? 'bg-amber-500 text-white' : 'bg-white border border-gray-200 text-gray-500'">
                Nueva (a crear)
              </button>
            </div>
            <SearchableSelect v-if="form.catModo === 'existente'" v-model="form.categoriaId" :options="categoriasOptions" placeholder="Buscar categoría..." />
            <div v-else class="flex gap-2">
              <input v-model="form.catNuevaNombre" type="text" maxlength="30" placeholder="Nombre de la categoría nueva"
                class="flex-1 px-3 py-2 text-sm border border-gray-200 rounded-lg focus:ring-2 focus:ring-[#0066cc] focus:border-transparent outline-none bg-white" />
              <input v-model="form.catNuevaIcono" type="text" maxlength="2" placeholder="🗂️"
                class="w-14 text-center px-2 py-2 text-sm border border-gray-200 rounded-lg outline-none focus:ring-2 focus:ring-[#0066cc] bg-white" />
            </div>
            <p v-if="form.catModo === 'nueva'" class="text-[11px] text-amber-600 mt-1">Se creará la categoría al aprobar la inscripción.</p>
          </div>
        </div>

        <!-- Ubicación y Responsable -->
        <div>
          <p class="text-xs text-gray-400 uppercase tracking-wide font-medium mb-3">Ubicación y Responsable</p>
          <div class="bg-gray-50/70 rounded-xl p-4 space-y-3">
            <div>
              <label class="block text-xs font-medium text-gray-500 mb-1">Ubicación Actual <span class="text-red-500">*</span></label>
              <input v-model="form.ubicacionActual" type="text" maxlength="30"
                class="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:ring-2 focus:ring-[#0066cc] focus:border-transparent outline-none bg-white" />
            </div>
            <div>
              <label class="block text-xs font-medium text-gray-500 mb-1">Encargado <span class="text-red-500">*</span></label>
              <div class="flex gap-1.5 mb-2">
                <button type="button" @click="form.encModo = 'existente'"
                  class="px-2.5 py-1 rounded-md text-xs font-medium transition"
                  :class="form.encModo === 'existente' ? 'bg-[#003d7a] text-white' : 'bg-white border border-gray-200 text-gray-500'">
                  Existente
                </button>
                <button type="button" @click="form.encModo = 'nueva'"
                  class="px-2.5 py-1 rounded-md text-xs font-medium transition"
                  :class="form.encModo === 'nueva' ? 'bg-amber-500 text-white' : 'bg-white border border-gray-200 text-gray-500'">
                  Nuevo (a crear)
                </button>
              </div>
              <SearchableSelect v-if="form.encModo === 'existente'" v-model="form.encargadoId" :options="encargadosOptions" placeholder="Buscar encargado..." />
              <div v-else class="grid grid-cols-2 gap-2">
                <input v-model="form.encNuevoNombre" type="text" maxlength="50" placeholder="Nombre del encargado"
                  class="px-3 py-2 text-sm border border-gray-200 rounded-lg focus:ring-2 focus:ring-[#0066cc] focus:border-transparent outline-none bg-white" />
                <input v-model="form.encNuevoRol" type="text" maxlength="30" placeholder="Cargo o rol"
                  class="px-3 py-2 text-sm border border-gray-200 rounded-lg focus:ring-2 focus:ring-[#0066cc] focus:border-transparent outline-none bg-white" />
              </div>
              <p v-if="form.encModo === 'nueva'" class="text-[11px] text-amber-600 mt-1">Se creará el encargado al aprobar la inscripción.</p>
            </div>
          </div>
        </div>

        <!-- Observaciones -->
        <div>
          <p class="text-xs text-gray-400 uppercase tracking-wide font-medium mb-3">Observaciones</p>
          <div class="bg-gray-50/70 rounded-xl p-4">
            <textarea v-model="form.observaciones" maxlength="200" rows="3"
              class="w-full px-3 py-2 text-sm border border-gray-200 rounded-lg focus:ring-2 focus:ring-[#0066cc] focus:border-transparent outline-none resize-none bg-white"
              placeholder="Notas adicionales sobre el activo..." />
            <p class="text-xs text-gray-400 mt-1 text-right">{{ (form.observaciones || '').length }}/200</p>
          </div>
        </div>

        <!-- Rechazo inline -->
        <div v-if="rechazando" class="space-y-2">
          <label class="block text-xs font-medium text-gray-500">Motivo del rechazo (opcional)</label>
          <textarea v-model="comentarioRechazo" rows="2"
            class="w-full px-3 py-2 text-sm border border-red-200 rounded-lg focus:ring-2 focus:ring-red-400/30 focus:border-red-400 outline-none resize-none"
            placeholder="Explica por qué se rechaza la solicitud..." />
        </div>

        <p v-if="error" class="text-sm text-red-600 bg-red-50 border border-red-100 p-3 rounded-lg">{{ error }}</p>
      </div>

      <!-- Footer -->
      <div class="flex flex-wrap gap-3 px-6 py-4 border-t border-gray-100 flex-shrink-0">
        <template v-if="rechazando">
          <button type="button" @click="rechazando = false; error = ''"
            class="flex-1 px-4 py-2.5 border border-gray-300 rounded-lg hover:bg-gray-100 transition font-medium">
            Cancelar
          </button>
          <button @click="confirmarRechazo" :disabled="loading"
            class="flex-1 px-4 py-2.5 bg-red-600 text-white rounded-lg hover:bg-red-700 transition font-medium disabled:bg-gray-400">
            Confirmar rechazo
          </button>
        </template>
        <template v-else>
          <button type="button" @click="$emit('close')"
            class="px-4 py-2.5 border border-gray-300 rounded-lg hover:bg-gray-100 transition font-medium">
            Cerrar
          </button>
          <button @click="guardarCambios" :disabled="loading || !hayModificaciones"
            class="px-4 py-2.5 border border-[#003d7a] text-[#003d7a] rounded-lg hover:bg-blue-50 transition font-medium disabled:opacity-40 disabled:cursor-not-allowed">
            Guardar cambios
          </button>
          <button @click="rechazando = true; error = ''" :disabled="loading"
            class="px-4 py-2.5 border border-red-200 text-red-600 rounded-lg hover:bg-red-50 transition font-medium disabled:opacity-50">
            Rechazar
          </button>
          <button @click="aprobar" :disabled="loading"
            class="flex-1 px-4 py-2.5 bg-green-600 text-white rounded-lg hover:bg-green-700 transition font-medium disabled:bg-gray-400 flex items-center justify-center gap-2">
            <svg v-if="loading" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"/>
            </svg>
            Aprobar y registrar
          </button>
        </template>
      </div>
    </div>
  </div>
  </Teleport>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import solicitudActivoService from '@/services/solicitudActivoService'
import SearchableSelect from '@/components/SearchableSelect.vue'
import { useDialog } from '@/composables/useDialog'

const props = defineProps({
  solicitud: { type: Object, required: true },
  categorias: { type: Array, default: () => [] },
  encargados: { type: Array, default: () => [] }
})
const emit = defineEmits(['close', 'resolved'])

const dialog = useDialog()
const loading = ref(false)
const error = ref('')
const rechazando = ref(false)
const comentarioRechazo = ref('')

const proponeCategoria = computed(() => !!props.solicitud?.categoriaNuevaNombre)
const proponeEncargado = computed(() => !!props.solicitud?.encargadoNuevoNombre)

const categoriasOptions = computed(() =>
  props.categorias.map(c => ({ value: c.id, label: `${c.icono ?? ''} ${c.nombre}`.trim() }))
)
const encargadosOptions = computed(() =>
  props.encargados.map(e => ({ value: e.id, label: `${e.nombre} — ${e.rol}` }))
)

const form = ref({
  placa: '', tipoPlaca: 'Institucional', marca: '', modelo: '', numSerial: '', articulo: '',
  observaciones: '', ubicacionActual: '',
  catModo: 'existente', categoriaId: '', catNuevaNombre: '', catNuevaIcono: '',
  encModo: 'existente', encargadoId: '', encNuevoNombre: '', encNuevoRol: ''
})

watch(() => props.solicitud, (s) => {
  if (!s) return
  form.value = {
    placa: s.placa,
    tipoPlaca: s.tipoPlaca,
    marca: s.marca,
    modelo: s.modelo,
    numSerial: s.numSerial,
    articulo: s.articulo,
    observaciones: s.observaciones || '',
    ubicacionActual: s.ubicacionActual,
    catModo: s.categoriaNuevaNombre ? 'nueva' : 'existente',
    categoriaId: s.categoriaId || '',
    catNuevaNombre: s.categoriaNuevaNombre || '',
    catNuevaIcono: s.categoriaNuevaIcono || '',
    encModo: s.encargadoNuevoNombre ? 'nueva' : 'existente',
    encargadoId: s.encargadoId || '',
    encNuevoNombre: s.encargadoNuevoNombre || '',
    encNuevoRol: s.encargadoNuevoRol || ''
  }
}, { immediate: true })

const hayModificaciones = computed(() => {
  const f = form.value
  const s = props.solicitud
  const catCambio = f.catModo !== (s.categoriaNuevaNombre ? 'nueva' : 'existente')
    || (f.catModo === 'existente'
        ? Number(f.categoriaId) !== s.categoriaId
        : (f.catNuevaNombre.trim() !== (s.categoriaNuevaNombre || '')
           || (f.catNuevaIcono.trim() || null) !== (s.categoriaNuevaIcono || null)))
  const encCambio = f.encModo !== (s.encargadoNuevoNombre ? 'nueva' : 'existente')
    || (f.encModo === 'existente'
        ? String(f.encargadoId) !== String(s.encargadoId || '')
        : (f.encNuevoNombre.trim() !== (s.encargadoNuevoNombre || '')
           || f.encNuevoRol.trim() !== (s.encargadoNuevoRol || '')))
  return f.placa !== s.placa ||
    f.tipoPlaca !== s.tipoPlaca ||
    f.marca !== s.marca ||
    f.modelo !== s.modelo ||
    f.numSerial !== s.numSerial ||
    f.articulo !== s.articulo ||
    (f.observaciones || '') !== (s.observaciones || '') ||
    f.ubicacionActual !== s.ubicacionActual ||
    catCambio || encCambio
})

function payload() {
  const cat = form.value.catModo === 'nueva'
  const enc = form.value.encModo === 'nueva'
  return {
    placa: form.value.placa?.trim(),
    tipoPlaca: form.value.tipoPlaca,
    marca: form.value.marca,
    modelo: form.value.modelo,
    numSerial: form.value.numSerial,
    articulo: form.value.articulo,
    observaciones: form.value.observaciones || null,
    ubicacionActual: form.value.ubicacionActual,
    categoriaId: cat ? null : Number(form.value.categoriaId),
    categoriaNuevaNombre: cat ? form.value.catNuevaNombre.trim() : null,
    categoriaNuevaIcono: cat ? (form.value.catNuevaIcono.trim() || null) : null,
    encargadoId: enc ? null : form.value.encargadoId,
    encargadoNuevoNombre: enc ? form.value.encNuevoNombre.trim() : null,
    encargadoNuevoRol: enc ? form.value.encNuevoRol.trim() : null
  }
}

function validar() {
  const f = form.value
  if (!f.placa?.trim()) return 'La placa es obligatoria.'
  if (!f.articulo?.trim()) return 'El artículo es obligatorio.'
  if (!f.numSerial?.trim()) return 'El número serial es obligatorio.'
  if (!f.marca?.trim()) return 'La marca es obligatoria.'
  if (!f.modelo?.trim()) return 'El modelo es obligatorio.'
  if (!f.ubicacionActual?.trim()) return 'La ubicación actual es obligatoria.'
  if (f.catModo === 'existente' && !f.categoriaId) return 'Selecciona una categoría o propón una nueva.'
  if (f.catModo === 'nueva' && !f.catNuevaNombre.trim()) return 'Escribe el nombre de la categoría nueva.'
  if (f.encModo === 'existente' && !f.encargadoId) return 'Selecciona un encargado o propón uno nuevo.'
  if (f.encModo === 'nueva' && (!f.encNuevoNombre.trim() || !f.encNuevoRol.trim())) return 'La propuesta de encargado necesita nombre y cargo/rol.'
  return null
}

async function guardarCambios() {
  error.value = validar()
  if (error.value) return
  loading.value = true
  try {
    await solicitudActivoService.editar(props.solicitud.id, payload())
    emit('resolved')
  } catch (e) {
    error.value = e.response?.data?.mensaje || 'No se pudieron guardar los cambios.'
  } finally {
    loading.value = false
  }
}

async function aprobar() {
  error.value = validar()
  if (error.value) return
  const extras = []
  if (form.value.catModo === 'nueva') extras.push(`la categoría «${form.value.catNuevaNombre.trim()}»`)
  if (form.value.encModo === 'nueva') extras.push(`el encargado «${form.value.encNuevoNombre.trim()}»`)
  const extraMsg = extras.length ? ` También se creará ${extras.join(' y ')}.` : ''
  const ok = await dialog.confirm({
    title: 'Aprobar y registrar activo',
    message: `El activo <strong>${form.value.placa}</strong> quedará registrado oficialmente en el inventario.${extraMsg} ¿Continuar?`,
    confirmText: 'Aprobar',
    type: 'info'
  })
  if (!ok) return
  loading.value = true
  try {
    // Persistir primero cualquier corrección para no perderla si el revisor editó campos.
    if (hayModificaciones.value) await solicitudActivoService.editar(props.solicitud.id, payload())
    await solicitudActivoService.aprobar(props.solicitud.id)
    emit('resolved')
  } catch (e) {
    error.value = e.response?.data?.mensaje || 'No se pudo aprobar la solicitud.'
  } finally {
    loading.value = false
  }
}

async function confirmarRechazo() {
  loading.value = true
  try {
    await solicitudActivoService.rechazar(props.solicitud.id, comentarioRechazo.value || null)
    emit('resolved')
  } catch (e) {
    error.value = e.response?.data?.mensaje || 'No se pudo rechazar la solicitud.'
  } finally {
    loading.value = false
  }
}

function formatFecha(iso) {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString('es-CR', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}
</script>
