<template>
  <div class="space-y-6">
    <!-- Header -->
    <div>
      <h1 class="text-3xl font-bold text-[#003d7a]">Manual de Usuario</h1>
      <p class="text-gray-600 mt-1">Guía de funcionalidades según tu perfil de acceso</p>
    </div>

    <!-- Perfil activo -->
    <div class="bg-white rounded-2xl shadow-md border border-blue-100 p-6 flex items-center gap-4">
      <div class="w-12 h-12 rounded-full flex items-center justify-center flex-shrink-0" :class="perfilColor.bg">
        <svg xmlns="http://www.w3.org/2000/svg" class="w-6 h-6" :class="perfilColor.icon" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
        </svg>
      </div>
      <div>
        <p class="text-xs text-gray-400 uppercase tracking-wide font-medium">Perfil actual</p>
        <p class="text-lg font-semibold text-gray-800">{{ rolDisplay }}</p>
      </div>
    </div>

    <!-- Capacidades del rol actual -->
    <div class="bg-white rounded-2xl shadow-md border border-blue-100 p-6 space-y-4">
      <h2 class="text-sm font-semibold text-[#003d7a] uppercase tracking-wider border-b border-gray-100 pb-3">
        ¿Qué podés hacer con este perfil?
      </h2>
      <ul class="space-y-2.5">
        <li v-for="item in capacidades" :key="item.texto" class="flex items-start gap-3">
          <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4 mt-0.5 flex-shrink-0 text-[#0066cc]" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7" />
          </svg>
          <span class="text-sm text-gray-700">{{ item.texto }}</span>
        </li>
      </ul>
    </div>

    <!-- Manuales disponibles -->
    <div class="bg-white rounded-2xl shadow-md border border-blue-100 p-6 space-y-4">
      <h2 class="text-sm font-semibold text-[#003d7a] uppercase tracking-wider border-b border-gray-100 pb-3">
        Manuales en PDF
      </h2>
      <p class="text-sm text-gray-500">
        Manuales en PDF. Los enlaces disponibles abren en una pestaña nueva (SharePoint institucional).
      </p>
      <div class="space-y-2">
        <a
          v-for="manual in manualesDisponibles"
          :key="manual.label"
          :href="manual.url || '#'"
          :target="manual.url ? '_blank' : undefined"
          :rel="manual.url ? 'noopener noreferrer' : undefined"
          class="flex items-center justify-between w-full px-4 py-3 rounded-xl border transition group"
          :class="manual.url
            ? 'border-blue-200 hover:border-[#003d7a] hover:bg-blue-50/50 cursor-pointer'
            : 'border-gray-200 bg-gray-50 cursor-not-allowed opacity-60'"
        >
          <div class="flex items-center gap-3">
            <svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5 flex-shrink-0"
              :class="manual.url ? 'text-red-500' : 'text-gray-400'"
              fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
            </svg>
            <div>
              <p class="text-sm font-medium text-gray-800">{{ manual.label }}</p>
              <p v-if="manual.descripcion" class="text-xs text-gray-400 mt-0.5">{{ manual.descripcion }}</p>
            </div>
          </div>
          <span v-if="!manual.url" class="text-xs text-gray-400 italic">Próximamente</span>
          <svg v-else xmlns="http://www.w3.org/2000/svg" class="w-4 h-4 text-gray-400 group-hover:text-[#003d7a] transition" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
          </svg>
        </a>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()

const rolDisplay = computed(() => {
  const map = {
    Administradora: 'Administradora',
    GTI: 'GTI',
    JefaAdministrativa: 'Jefa Administrativa',
    Invitado: 'Invitado'
  }
  return map[auth.permisos] ?? auth.permisos
})

const perfilColor = computed(() => {
  const map = {
    Administradora:    { bg: 'bg-purple-100', icon: 'text-purple-600' },
    GTI:               { bg: 'bg-blue-100',   icon: 'text-blue-600'   },
    JefaAdministrativa:{ bg: 'bg-green-100',  icon: 'text-green-600'  },
    Invitado:          { bg: 'bg-gray-100',   icon: 'text-gray-500'   }
  }
  return map[auth.permisos] ?? { bg: 'bg-gray-100', icon: 'text-gray-500' }
})

const capacidadesPorRol = {
  Administradora: [
    { texto: 'Ver, crear, editar y eliminar activos del inventario' },
    { texto: 'Aprobar o rechazar cambios de placa de activos' },
    { texto: 'Crear nuevos usuarios y generar contraseñas temporales' },
    { texto: 'Restablecer contraseñas y desbloquear cuentas' },
    { texto: 'Aprobar, corregir o rechazar solicitudes de cambio y de inscripción de activos enviadas por la Jefa Administrativa' },
    { texto: 'Aprobar la eliminación definitiva de activos que hayan cumplido el período en desecho' },
    { texto: 'Gestionar categorías de activos (crear, editar, eliminar)' },
    { texto: 'Ver el historial completo de acciones del sistema' },
    { texto: 'Acceder a la gestión de activos en estado de desecho' },
    { texto: 'Importar activos de forma masiva desde un archivo Excel o CSV' }
  ],
  GTI: [
    { texto: 'Ver, crear y editar activos del inventario' },
    { texto: 'Cambiar la ubicación y el encargado de cualquier activo' },
    { texto: 'Cambiar el estado de un activo (Activo, Mantenimiento, Desecho)' },
    { texto: 'Aprobar, corregir o rechazar solicitudes de cambio y de inscripción de activos enviadas por la Jefa Administrativa' },
    { texto: 'Ver y gestionar la lista de encargados' },
    { texto: 'Ver el historial de movimientos y cambios del sistema' },
    { texto: 'Importar activos de forma masiva desde un archivo Excel o CSV' }
  ],
  JefaAdministrativa: [
    { texto: 'Ver el inventario completo de activos' },
    { texto: 'Consultar el detalle individual de cada activo' },
    { texto: 'Inscribir activos nuevos, uno a uno, para que Administración o GTI los apruebe' },
    { texto: 'Proponer, dentro de una inscripción, una categoría o un encargado nuevos (se crean solo al aprobar)' },
    { texto: 'Solicitar cambios en un activo (artículo, marca, modelo, ubicación, encargado)' },
    { texto: 'Solicitar el envío de un activo a desecho' },
    { texto: 'Dar seguimiento al estado de tus solicitudes de inscripción y de cambio' },
    { texto: 'Ver el historial de movimientos de activos' },
    { texto: 'Descargar la plantilla de importación masiva para entregarla a quien haga cargas masivas' }
  ],
  Invitado: [
    { texto: 'Ver el inventario de activos en modo solo lectura' },
    { texto: 'Consultar el catálogo de categorías' }
  ]
}

const capacidades = computed(() => capacidadesPorRol[auth.permisos] ?? [])

const todosLosManuales = [
  {
    key: 'admin',
    label: 'Manual de Administradora',
    descripcion: 'Gestión de usuarios, activos, categorías y aprobaciones',
    url: 'https://6f33fa7f78ea46e2aaca-my.sharepoint.com/:b:/g/personal/soporte_eic_ucr_ac_cr/IQBW4jvlsm7KSZVnps-JyjFRAYjf2__04thH56fGz1PMVXk?e=0WdXUf',
    roles: ['Administradora']
  },
  {
    key: 'gti',
    label: 'Manual de GTI',
    descripcion: 'Inventario, encargados, desecho e importación masiva',
    url: 'https://6f33fa7f78ea46e2aaca-my.sharepoint.com/:b:/g/personal/soporte_eic_ucr_ac_cr/IQC8CAWL62dEQIfKkefK7A2VASYxpQiONW9KbUI4r-9ubr8?e=6l9suv',
    roles: ['Administradora', 'GTI']
  },
  {
    key: 'jefa',
    label: 'Manual de Jefa Administrativa',
    descripcion: 'Inscripción de activos y solicitudes de cambio',
    url: 'https://6f33fa7f78ea46e2aaca-my.sharepoint.com/:b:/g/personal/soporte_eic_ucr_ac_cr/IQBtCGvKbP9aQJESdG7HzAJCAQa6drCU2eimBZ1WfAW-Uh4?e=TP21hC',
    roles: ['Administradora', 'JefaAdministrativa']
  },
  {
    key: 'invitado',
    label: 'Manual de Invitado',
    descripcion: 'Navegación y consulta del inventario',
    url: 'https://6f33fa7f78ea46e2aaca-my.sharepoint.com/:b:/g/personal/soporte_eic_ucr_ac_cr/IQDeoLnPMUmJQbTqddEpnYPsAVkweZCEiNVG4l1UrBpgBUU?e=xTvN9m',
    roles: ['Administradora', 'Invitado']
  },
  {
    key: 'tecnico',
    label: 'Manual Técnico',
    descripcion: 'Arquitectura, despliegue, base de datos y configuración del sistema',
    url: 'https://6f33fa7f78ea46e2aaca-my.sharepoint.com/:b:/g/personal/soporte_eic_ucr_ac_cr/IQBU84UjzLcKTKpYoN2fNj7zAdDO0bUnAMtMjW7Fr57T80c?e=sXzpcH',
    roles: ['Administradora']
  }
]

const manualesDisponibles = computed(() =>
  todosLosManuales.filter(m => m.roles.includes(auth.permisos))
)
</script>
