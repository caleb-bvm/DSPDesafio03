# Caso 11: token vencido en EquiposMVC

Verificado el 17 de octubre de 2026 en la instalación local.

1. Se configuró temporalmente el JWT de la API con expiración de 1 minuto y
   tolerancia de reloj cero. No se cambió el código del cliente.
2. Se inició sesión como Admin en EquiposMVC y el inventario cargó.
3. Después de 67 segundos, se solicitó de nuevo `/Equipo/Index` con la misma
   cookie del navegador.
4. La llamada del cliente a `/api/equipos` recibió HTTP 401. La respuesta
   final del navegador fue la página **Iniciar sesión** con el aviso:
   «Tu sesión venció. Inicia sesión de nuevo.»

El código de `AppController.HandleError` llama a `Session.Clear()` antes de
redirigir. En esta prueba se observó el 401 y la pantalla final; no se capturó
directamente el contenido interno de Session ni la cabecera Location
intermedia. Después se restauró la expiración normal de 60 minutos.
