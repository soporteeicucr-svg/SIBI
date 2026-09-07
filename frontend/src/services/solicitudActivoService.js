import api from './api'

export default {
  listar: (estado) =>
    api.get('/solicitudactivo', { params: estado ? { estado } : {} }),

  listarMias: (estado) =>
    api.get('/solicitudactivo/mis', { params: estado ? { estado } : {} }),

  contarPendientes: () =>
    api.get('/solicitudactivo/pendientes/count'),

  crear: (data) =>
    api.post('/solicitudactivo', data),

  editar: (id, data) =>
    api.put(`/solicitudactivo/${id}`, data),

  aprobar: (id) =>
    api.post(`/solicitudactivo/${id}/aprobar`),

  rechazar: (id, comentario) =>
    api.post(`/solicitudactivo/${id}/rechazar`, { comentario })
}
