# Documento de Requerimientos Funcionales
## Sistema de Gestión y Seguimiento de Becas Trabajo - Universidad

**Proyecto:** Sistema de Gestión de Becas Trabajo
**Versión:** 2.0 - Completa
**Fecha:** 2026-02-13
**Autor:** Equipo de Desarrollo

---

## 📋 Índice

1. [Descripción General del Sistema](#1-descripción-general-del-sistema)
2. [Actores del Sistema](#2-actores-del-sistema)
3. [Glosario de Términos](#3-glosario-de-términos)
4. [Subsistemas](#4-subsistemas)
5. [Requerimientos Funcionales](#5-requerimientos-funcionales)
   - [5.1 Autenticación y Autorización](#51-autenticación-y-autorización)
   - [5.2 Gestión de Ciclos/Semestres](#52-gestión-de-ciclossemestres)
   - [5.3 Proceso de Selección](#53-proceso-de-selección)
   - [5.4 Gestión de Ubicaciones](#54-gestión-de-ubicaciones)
   - [5.5 Sistema de Tracking de Horas](#55-sistema-de-tracking-de-horas)
   - [5.6 Gestión de Ausencias](#56-gestión-de-ausencias)
   - [5.7 Generación de Documentos](#57-generación-de-documentos)
   - [5.8 Sistema de Notificaciones](#58-sistema-de-notificaciones)
   - [5.9 Reportes y Consultas](#59-reportes-y-consultas)
   - [5.10 Historial y Auditoría](#510-historial-y-auditoría)
6. [Reglas de Negocio](#6-reglas-de-negocio)
7. [Requerimientos No Funcionales](#7-requerimientos-no-funcionales)
8. [Modelo de Datos](#8-modelo-de-datos)
9. [Flujos de Procesos](#9-flujos-de-procesos)

---

## 1. Descripción General del Sistema

### 1.1 Propósito
Sistema integral para gestionar el ciclo completo de becas trabajo universitarias, desde la postulación y selección hasta el seguimiento diario de horas trabajadas y generación de documentación oficial.

### 1.2 Alcance
El sistema abarca:
- **Gestión de ciclos semestrales** de becas trabajo
- **Proceso de selección** de nuevos becas (postulación, entrevista, asignación)
- **Renovación automática** para becas con buen desempeño
- **Tracking en tiempo real** de horas trabajadas con evidencia fotográfica
- **Supervisión y aprobación** de jornadas por personal de biblioteca
- **Gestión de ausencias** y adelanto de horas
- **Generación de bitácoras oficiales** y escarapelas
- **Sistema multi-dependencia** (preparado para escalar más allá de biblioteca)

### 1.3 Usuarios Objetivo
- **Administradores** de biblioteca/dependencia
- **Supervisores** de zona (empleados de biblioteca)
- **Estudiantes becados** (Becas Trabajo)
- **Postulantes** (futuros becas)

---

## 2. Actores del Sistema

### 2.1 Administrador
**Rol:** ADMIN
**Descripción:** Personal administrativo de la biblioteca/dependencia con control total del sistema.

**Responsabilidades:**
- Crear y gestionar ciclos semestrales
- Configurar ubicaciones y horarios
- Gestionar proceso de selección completo
- Asignar becas a ubicaciones
- Asignar supervisores a zonas
- Generar documentos oficiales (bitácoras, escarapelas)
- Revisar reportes y métricas
- Gestionar usuarios del sistema
- Aprobar solicitudes especiales

### 2.2 Supervisor de Zona
**Rol:** SUPERVISOR
**Descripción:** Empleado de biblioteca responsable de una o más ubicaciones.

**Responsabilidades:**
- Supervisar becas asignados a su(s) zona(s)
- Aprobar registros de entrada/salida (check-in/check-out)
- Gestionar solicitudes de ausencia
- Aprobar adelanto de horas
- Reportar incidencias
- Revisar evidencia fotográfica de jornadas
- Firmar bitácoras digitales

### 2.3 Beca Trabajo
**Rol:** BECA
**Descripción:** Estudiante universitario que trabaja bajo el programa de beca trabajo.

**Responsabilidades:**
- Registrar entrada/salida con evidencia fotográfica
- Cumplir horario asignado
- Reportar ausencias con anticipación
- Solicitar adelanto de horas
- Mantener evidencia de trabajo realizado
- Consultar horas acumuladas
- Actualizar horario cada semestre (si es renovación)

### 2.4 Postulante
**Rol:** POSTULANTE
**Descripción:** Estudiante que aplica para obtener una beca trabajo.

**Responsabilidades:**
- Completar formulario de postulación
- Subir horario académico actualizado
- Subir fotografía
- Asistir a entrevista (si es seleccionado)
- Consultar estado de postulación

---

## 3. Glosario de Términos

| Término | Definición |
|---------|------------|
| **Beca Trabajo** | Estudiante que trabaja en la biblioteca/dependencia bajo programa de apoyo financiero |
| **Ciclo/Semestre** | Periodo académico (aprox. 16 semanas) durante el cual opera el programa |
| **Ubicación/Zona** | Espacio físico dentro de la biblioteca (ej: Sala de Lectura, Área de Cómputo) |
| **Supervisor** | Empleado permanente encargado de supervisar a becas en una ubicación |
| **Check-in** | Registro de entrada al iniciar jornada laboral |
| **Check-out** | Registro de salida al finalizar jornada laboral |
| **Jornada** | Periodo de trabajo de un beca (típicamente 2-4 horas) |
| **Bitácora** | Documento oficial que registra todas las horas trabajadas en un ciclo |
| **Escarapela** | Carnet/credencial imprimible que identifica al beca |
| **Adelanto de Horas** | Solicitud para trabajar horas adicionales fuera del horario regular |
| **Postulante** | Estudiante que aplica para ser beca trabajo |
| **Renovación** | Proceso simplificado para becas anteriores que desean continuar |

---

## 4. Subsistemas

El sistema se divide en 10 subsistemas principales:

| Código | Subsistema | Descripción |
|--------|-----------|-------------|
| **AUTH** | Autenticación y Autorización | Login, roles, permisos, OAuth |
| **CICLO** | Gestión de Ciclos/Semestres | Crear, configurar, cerrar ciclos |
| **SEL** | Proceso de Selección | Postulación, entrevista, asignación |
| **UBIC** | Gestión de Ubicaciones | Zonas, horarios, asignaciones |
| **TRACK** | Tracking de Horas | Check-in/out, registro de jornadas |
| **AUS** | Gestión de Ausencias | Reportes, justificaciones, conteo |
| **DOC** | Generación de Documentos | Bitácoras, escarapelas, reportes PDF |
| **NOTIF** | Sistema de Notificaciones | Emails, notificaciones push |
| **REP** | Reportes y Consultas | Dashboards, métricas, consultas |
| **HIST** | Historial y Auditoría | Logs, historial de cambios |

---

## 5. Requerimientos Funcionales

### Formato de Requerimiento

```
RF-XXX | Nombre del Requerimiento
Subsistema: [Código]
Prioridad: [Alta | Media | Baja]
Roles: [Roles que interactúan]
Dependencias: [RF-YYY, RF-ZZZ]

Descripción:
[Descripción detallada]

Criterios de Aceptación:
1. [Criterio 1]
2. [Criterio 2]
...

Notas:
[Información adicional]
```

---

## 5.1 Autenticación y Autorización

### RF-001 | Login con Email y Contraseña
**Subsistema:** AUTH
**Prioridad:** Alta
**Roles:** Todos
**Dependencias:** Ninguna

**Descripción:**
Los usuarios deben poder autenticarse en el sistema utilizando su correo institucional y contraseña.

**Criterios de Aceptación:**
1. El login utiliza email (no username)
2. Contraseñas almacenadas de forma segura (hashed con bcrypt/argon2)
3. Genera token JWT con expiración configurable (ej: 24h)
4. Retorna información del usuario: ID, nombre, email, rol
5. Actualiza fecha de último login
6. Bloquea cuenta tras 5 intentos fallidos consecutivos (15 minutos)
7. Muestra mensajes de error genéricos por seguridad

**Notas:**
Token JWT debe incluir claims: userId, email, role, permissions.

---

### RF-002 | Login con OAuth (Google)
**Subsistema:** AUTH
**Prioridad:** Media
**Roles:** BECA, POSTULANTE
**Dependencias:** RF-001

**Descripción:**
Estudiantes pueden autenticarse usando su cuenta institucional de Google (opcional al login tradicional).

**Criterios de Aceptación:**
1. Integración con Google OAuth 2.0
2. Solo permite emails del dominio institucional (@universidad.edu)
3. Si es primera vez, crea usuario automáticamente
4. Si ya existe usuario con ese email, hace login directo
5. Genera token JWT igual que login tradicional
6. Mapea automáticamente rol inicial como POSTULANTE

**Notas:**
Considerar Microsoft OAuth para futuro.

---

### RF-003 | Gestión de Roles y Permisos
**Subsistema:** AUTH
**Prioridad:** Alta
**Roles:** ADMIN
**Dependencias:** RF-001

**Descripción:**
El sistema debe manejar 3 roles con permisos específicos. Solo administradores pueden cambiar roles de usuarios.

**Criterios de Aceptación:**
1. **Roles definidos:**
   - ADMIN: Acceso total
   - SUPERVISOR: Gestión de su(s) zona(s) y becas asignados
   - BECA: Funciones de tracking personal
2. Cada endpoint valida rol requerido
3. Admin puede asignar/revocar roles
4. Cambio de rol genera log de auditoría
5. Un usuario solo puede tener un rol a la vez

**Notas:**
POSTULANTE no es un rol persistente, es un estado temporal hasta ser seleccionado.

---

### RF-004 | Recuperación de Contraseña
**Subsistema:** AUTH
**Prioridad:** Media
**Roles:** Todos
**Dependencias:** RF-001, RF-043 (email)

**Descripción:**
Usuarios pueden recuperar su contraseña mediante enlace enviado a su correo.

**Criterios de Aceptación:**
1. Usuario solicita recuperación con su email
2. Sistema genera token de un solo uso con expiración (1 hora)
3. Envía email con enlace a página de reset
4. Usuario crea nueva contraseña
5. Token se invalida tras usarse
6. Notifica cambio de contraseña por email

---

### RF-005 | Cambio de Contraseña (Usuario Autenticado)
**Subsistema:** AUTH
**Prioridad:** Baja
**Roles:** Todos
**Dependencias:** RF-001

**Descripción:**
Usuario autenticado puede cambiar su contraseña actual.

**Criterios de Aceptación:**
1. Requiere contraseña actual para verificación
2. Nueva contraseña debe cumplir política de seguridad
3. Invalida todos los tokens JWT existentes
4. Genera nuevo token
5. Notifica cambio por email

---

## 5.2 Gestión de Ciclos/Semestres

### RF-006 | Crear Nuevo Ciclo Semestral
**Subsistema:** CICLO
**Prioridad:** Alta
**Roles:** ADMIN
**Dependencias:** RF-001

**Descripción:**
Administrador crea un nuevo ciclo/semestre para iniciar proceso de becas trabajo.

**Criterios de Aceptación:**
1. **Datos requeridos:**
   - Nombre del ciclo (ej: "2024-1", "Enero-Mayo 2024")
   - Dependencia (ej: "Biblioteca", "Centro de Cómputo")
   - Fecha inicio del ciclo
   - Fecha fin del ciclo
   - Total de becas disponibles
   - Fecha límite de postulaciones
   - Fecha de entrevistas
   - Fecha de selección final
2. Solo puede haber un ciclo activo por dependencia
3. Al crear ciclo, estado inicial: "Configuración"
4. Valida que fechas sean coherentes (inicio < fin, etc.)
5. Genera log de auditoría

**Notas:**
Ciclo en "Configuración" permite setup de ubicaciones antes de abrir postulaciones.

---

### RF-007 | Configurar Ciclo
**Subsistema:** CICLO
**Prioridad:** Alta
**Roles:** ADMIN
**Dependencias:** RF-006, RF-023 (ubicaciones)

**Descripción:**
Antes de abrir postulaciones, admin configura ubicaciones, horarios y supervisores del ciclo.

**Criterios de Aceptación:**
1. Admin puede modificar:
   - Fechas del ciclo
   - Total de becas disponibles
   - Ubicaciones activas para este ciclo
   - Horarios por ubicación
   - Supervisores asignados
2. Validación: suma de becas por ubicación ≤ total becas del ciclo
3. No se puede modificar si ciclo ya está "Activo" (solo extender fechas)

---

### RF-008 | Abrir Periodo de Postulaciones
**Subsistema:** CICLO
**Prioridad:** Alta
**Roles:** ADMIN
**Dependencias:** RF-007

**Descripción:**
Admin activa el ciclo para que postulantes puedan registrarse.

**Criterios de Aceptación:**
1. Valida que ciclo esté completamente configurado:
   - Al menos una ubicación activa
   - Fechas definidas
   - Total becas > 0
2. Cambia estado del ciclo a: "Postulaciones Abiertas"
3. Genera evento de notificación (si hay suscriptores)
4. Postulantes pueden empezar a registrarse

---

### RF-009 | Cerrar Periodo de Postulaciones
**Subsistema:** CICLO
**Prioridad:** Alta
**Roles:** ADMIN
**Dependencias:** RF-008

**Descripción:**
Admin cierra periodo de postulaciones para pasar a fase de revisión/entrevistas.

**Criterios de Aceptación:**
1. Cambia estado del ciclo a: "Postulaciones Cerradas"
2. Postulantes ya no pueden registrarse
3. Admin puede revisar lista completa de postulantes
4. Se puede reabrir manualmente si es necesario

---

### RF-010 | Extender Fechas del Ciclo
**Subsistema:** CICLO
**Prioridad:** Media
**Roles:** ADMIN
**Dependencias:** RF-006

**Descripción:**
Admin puede extender fechas límite del ciclo si hay retrasos.

**Criterios de Aceptación:**
1. Puede extender:
   - Fecha límite de postulaciones
   - Fecha de entrevistas
   - Fecha de selección
   - Fecha fin del ciclo
2. No puede reducir fechas si ya pasaron
3. Notifica a usuarios afectados por cambio
4. Genera log de auditoría

---

### RF-011 | Cerrar Ciclo Semestral
**Subsistema:** CICLO
**Prioridad:** Alta
**Roles:** ADMIN
**Dependencias:** RF-006, RF-041 (bitácoras)

**Descripción:**
Al finalizar el semestre, admin cierra oficialmente el ciclo.

**Criterios de Aceptación:**
1. Valida que:
   - Fecha actual ≥ fecha fin del ciclo
   - Todas las bitácoras estén generadas
   - No haya jornadas pendientes de aprobar
2. Cambia estado del ciclo a: "Cerrado"
3. Congela datos (no se pueden modificar registros de horas)
4. Becas con buen desempeño quedan marcados para renovación en siguiente ciclo
5. Genera reporte final del ciclo

**Notas:**
Criterio de "buen desempeño": ≥90% asistencia, ≥95% horas cumplidas.

---

### RF-012 | Ver Historial de Ciclos
**Subsistema:** CICLO
**Prioridad:** Media
**Roles:** ADMIN
**Dependencias:** RF-011

**Descripción:**
Admin puede consultar información de ciclos pasados.

**Criterios de Aceptación:**
1. Lista todos los ciclos (activos y cerrados)
2. Por cada ciclo muestra:
   - Nombre, dependencia
   - Fechas inicio/fin
   - Total becas seleccionados
   - Estado (Configuración/Activo/Cerrado)
   - Métricas básicas (postulantes, seleccionados, horas totales)
3. Permite filtrar por: dependencia, año, semestre
4. Acceso a reportes y documentos de ciclos cerrados

---

## 5.3 Proceso de Selección

### RF-013 | Subir Lista de Postulantes (Excel)
**Subsistema:** SEL
**Prioridad:** Alta
**Roles:** ADMIN
**Dependencias:** RF-008

**Descripción:**
Admin sube archivo Excel con lista oficial de postulantes obtenida del sistema institucional.

**Criterios de Aceptación:**
1. Acepta archivo Excel (.xlsx)
2. **Estructura esperada:**
   - Código de estudiante
   - Nombre completo
   - Email institucional
   - Dependencia solicitada
   - Promedio académico
3. Filtra postulantes de la dependencia correspondiente
4. Ordena por promedio (descendente)
5. Muestra preview antes de confirmar
6. Valida:
   - Emails únicos
   - Códigos únicos
   - Formato de datos correcto

**Notas:**
Este archivo viene del sistema de registro centralizado de la universidad.

---

### RF-014 | Confirmar Lista y Crear Usuarios Postulantes
**Subsistema:** SEL
**Prioridad:** Alta
**Roles:** ADMIN
**Dependencias:** RF-013, RF-043 (email)

**Descripción:**
Tras revisar preview, admin confirma lista. Sistema crea usuarios automáticamente.

**Criterios de Aceptación:**
1. Por cada postulante:
   - Crea usuario con email institucional
   - Genera contraseña aleatoria (12 caracteres)
   - Asigna rol temporal: POSTULANTE
   - Crea registro en tabla `Postulaciones` vinculado al ciclo
   - Estado inicial: "Pendiente de Completar Formulario"
2. Guarda credenciales en archivo CSV seguro
3. Envía email a cada postulante con:
   - Credenciales de acceso
   - Enlace al sistema
   - Fecha límite para completar formulario
4. Genera log de auditoría

---

### RF-015 | Postulante Completa Formulario
**Subsistema:** SEL
**Prioridad:** Alta
**Roles:** BECA (postulante)
**Dependencias:** RF-014

**Descripción:**
Postulante inicia sesión por primera vez y completa formulario de postulación.

**Criterios de Aceptación:**
1. **Datos del formulario:**
   - Nombres y apellidos
   - Fecha de nacimiento
   - Dirección
   - Género
   - Carrera
   - Fotografía (formato: JPG/PNG, max 2MB)
   - Horario académico (PDF generado por sistema institucional)
   - Estudios adicionales (opcional)
   - Motivación (texto, max 500 caracteres)
2. Validaciones:
   - PDF de horario cumple formato estándar (RF-016)
   - Todos los campos obligatorios completos
3. Al enviar:
   - Estado cambia a: "Formulario Completado"
   - No puede editar después (salvo solicitud a admin)
4. Postulante puede consultar su estado en cualquier momento

---

### RF-016 | Validar Formato de Horario PDF
**Subsistema:** SEL
**Prioridad:** Alta
**Roles:** Sistema (backend)
**Dependencias:** RF-015

**Descripción:**
Sistema valida que el horario PDF cumpla con formato institucional estándar.

**Criterios de Aceptación:**
1. **Validaciones del PDF:**
   - Máximo 2 páginas
   - Contiene tabla con columnas: Hora, Lunes, Martes, Miércoles, Jueves, Viernes, Sábado
   - Filas de horario: 06:00-07:00 hasta 21:00-22:00 (16 franjas)
   - Incluye código y nombre del estudiante en header
2. Si formato es incorrecto, muestra error específico
3. Si es correcto, extrae y guarda horario en formato estructurado (JSON)

**Notas:**
Usar biblioteca de procesamiento PDF (iText7, QuestPDF, o similar en .NET).

---

### RF-017 | Matching Automático Postulante-Ubicación
**Subsistema:** SEL
**Prioridad:** Alta
**Roles:** Sistema (backend)
**Dependencias:** RF-016, RF-023

**Descripción:**
Sistema calcula compatibilidad entre horario del postulante y horarios de ubicaciones disponibles.

**Criterios de Aceptación:**
1. Algoritmo compara:
   - Horarios libres del postulante (celdas vacías en PDF)
   - Horarios de operación de cada ubicación
2. Calcula porcentaje de compatibilidad por ubicación
3. Ordena ubicaciones por compatibilidad (descendente)
4. Guarda resultados en tabla `CompatibilidadUbicacion`
5. Admin puede ver estos resultados al hacer asignaciones

**Notas:**
Prioridad: becas que renuevan tienen prioridad si su horario sigue siendo compatible.

---

### RF-018 | Gestionar Proceso de Entrevistas
**Subsistema:** SEL
**Prioridad:** Media
**Roles:** ADMIN, SUPERVISOR
**Dependencias:** RF-015

**Descripción:**
Admin programa y registra entrevistas con postulantes seleccionados para siguiente fase.

**Criterios de Aceptación:**
1. Admin filtra postulantes por:
   - Formulario completado
   - Compatibilidad con ubicaciones
   - Promedio académico
2. Selecciona postulantes para entrevistar
3. Por cada postulante:
   - Asigna fecha/hora de entrevista
   - Asigna entrevistador (Admin o Supervisor)
   - Envía notificación por email
4. Durante/después de entrevista:
   - Registra notas
   - Califica (ej: 1-5 estrellas)
   - Marca como: Aprobado / En espera / Rechazado
5. Postulante puede ver si fue seleccionado para entrevista

**Notas:**
Futuro: integración con calendario (Google Calendar, Outlook).

---

### RF-019 | Asignar Postulantes a Ubicaciones
**Subsistema:** SEL
**Prioridad:** Alta
**Roles:** ADMIN
**Dependencias:** RF-017, RF-018

**Descripción:**
Admin asigna postulantes aprobados a ubicaciones específicas según compatibilidad y necesidades.

**Criterios de Aceptación:**
1. Vista de asignación muestra:
   - Lista de postulantes aprobados
   - Compatibilidad por ubicación (%)
   - Ubicaciones con plazas disponibles
2. Admin arrastra/asigna postulante a ubicación
3. Validaciones:
   - No exceder total de becas por ubicación
   - No exceder total de becas del ciclo
   - Preferencia a becas que renuevan (si aplican)
4. Al asignar:
   - Genera horario de trabajo basado en compatibilidad
   - Reserva slots de horario en ubicación
5. Puede reasignar antes de confirmar selección final

---

### RF-020 | Confirmar Selección Final
**Subsistema:** SEL
**Prioridad:** Alta
**Roles:** ADMIN
**Dependencias:** RF-019, RF-043

**Descripción:**
Admin confirma lista final de becas seleccionados. Cambia sus roles y notifica resultados.

**Criterios de Aceptación:**
1. Valida que todas las ubicaciones tengan becas asignados
2. Por cada seleccionado:
   - Cambia rol de usuario: POSTULANTE → BECA
   - Crea registro en tabla `BecaTrabajo` para este ciclo
   - Asigna ubicación y horario
   - Estado: "Seleccionado - Activo"
   - Envía email de felicitación con:
     - Ubicación asignada
     - Supervisor asignado
     - Horario de trabajo
     - Fecha de inicio
3. Por cada NO seleccionado:
   - Envía email de notificación
   - Mantiene usuario (puede postular en futuro)
4. Cambia estado del ciclo a: "Activo"
5. Genera documentación inicial (escarapelas)

---

### RF-021 | Proceso de Renovación (Becas Anteriores)
**Subsistema:** SEL
**Prioridad:** Alta
**Roles:** ADMIN, BECA
**Dependencias:** RF-011

**Descripción:**
Becas de semestres anteriores con buen desempeño pueden renovar sin pasar por proceso completo.

**Criterios de Aceptación:**
1. Al iniciar nuevo ciclo, sistema identifica becas elegibles para renovación:
   - Buen desempeño en ciclo anterior (RF-011)
   - Usuario sigue activo
2. Admin envía invitación de renovación por email
3. Beca interesado:
   - Inicia sesión
   - Sube horario actualizado del nuevo semestre (PDF)
   - Confirma interés en renovar
4. Sistema:
   - Valida formato de nuevo horario
   - Calcula compatibilidad con ubicación anterior
   - Si es compatible (≥70%): asignación automática a misma ubicación
   - Si no es compatible: pasa a pool de postulantes normales
5. Asignación de renovaciones ocurre **antes** de proceso normal
6. Plazas restantes se abren para postulantes nuevos

**Notas:**
Prioridad de renovación es clave para retener talento y dar estabilidad a becas.

---

### RF-022 | Rechazar/Cancelar Postulación
**Subsistema:** SEL
**Prioridad:** Baja
**Roles:** ADMIN, BECA (postulante)
**Dependencias:** RF-015

**Descripción:**
Postulante puede cancelar su postulación. Admin puede rechazar postulantes.

**Criterios de Aceptación:**
1. **Postulante cancela:**
   - Puede hacerlo antes de cierre de postulaciones
   - Cambia estado a: "Cancelado por Usuario"
   - Mantiene usuario (puede postular en futuro)
2. **Admin rechaza:**
   - Puede rechazar con motivo (texto)
   - Envía email con notificación
   - Cambia estado a: "Rechazado"

---

## 5.4 Gestión de Ubicaciones

### RF-023 | Crear Ubicación
**Subsistema:** UBIC
**Prioridad:** Alta
**Roles:** ADMIN
**Dependencias:** RF-006

**Descripción:**
Admin crea una nueva ubicación/zona dentro de la biblioteca o dependencia.

**Criterios de Aceptación:**
1. **Datos requeridos:**
   - Nombre de ubicación (único por dependencia)
   - Descripción
   - Total de becas asignables
   - Imagen/foto del lugar
   - Tipo de horario (RF-024)
   - Horarios de operación (RF-025)
2. Validación: nombre único
3. Al crear, estado inicial: "Inactiva"
4. Admin puede activarla para ciclo específico

---

### RF-024 | Tipos de Horario de Ubicación
**Subsistema:** UBIC
**Prioridad:** Alta
**Roles:** Sistema
**Dependencias:** RF-023

**Descripción:**
Sistema soporta 3 tipos de horarios para ubicaciones.

**Criterios de Aceptación:**
1. **Tipos definidos:**
   - **Unificado excluyendo sábado:** Todos los becas trabajan el mismo horario, Lunes-Viernes
   - **Unificado incluyendo sábado:** Todos los becas trabajan el mismo horario, Lunes-Sábado
   - **Personalizado:** Cada slot de horario puede tener diferente cantidad de becas
2. Tipo de horario afecta validaciones de asignación

---

### RF-025 | Definir Horarios de Ubicación
**Subsistema:** UBIC
**Prioridad:** Alta
**Roles:** ADMIN
**Dependencias:** RF-024

**Descripción:**
Admin define slots de horario en los que la ubicación requiere becas.

**Criterios de Aceptación:**
1. **Estructura de horario:**
   ```json
   {
     "scheduleType": "Personalizado",
     "schedule": [
       {
         "days": ["Lunes", "Martes", "Miércoles"],
         "hours": [
           {
             "start": "08:00",
             "end": "10:00",
             "becasRequired": 2
           },
           {
             "start": "14:00",
             "end": "16:00",
             "becasRequired": 1
           }
         ]
       },
       {
         "days": ["Jueves", "Viernes"],
         "hours": [
           {
             "start": "10:00",
             "end": "12:00",
             "becasRequired": 2
           }
         ]
       }
     ]
   }
   ```
2. Validaciones:
   - Horas válidas (formato HH:MM)
   - start < end
   - No solapamiento de slots
   - Suma total de becas requeridos ≤ total de ubicación
3. Slots se usan para matching con horarios de postulantes

---

### RF-026 | Asignar Supervisor a Ubicación
**Subsistema:** UBIC
**Prioridad:** Alta
**Roles:** ADMIN
**Dependencias:** RF-023, RF-003

**Descripción:**
Admin asigna un empleado de biblioteca (supervisor) como responsable de una ubicación.

**Criterios de Aceptación:**
1. Valida que usuario tenga rol SUPERVISOR
2. Un supervisor puede estar asignado a múltiples ubicaciones
3. Una ubicación debe tener al menos un supervisor
4. Supervisor recibe notificación de asignación
5. Supervisor puede ver becas asignados a su(s) ubicación(es)

---

### RF-027 | Actualizar Ubicación
**Subsistema:** UBIC
**Prioridad:** Media
**Roles:** ADMIN
**Dependencias:** RF-023

**Descripción:**
Admin puede modificar datos de una ubicación existente.

**Criterios de Aceptación:**
1. **Campos editables:**
   - Descripción
   - Imagen
   - Supervisor asignado
   - (Horarios solo si no hay becas asignados actualmente)
2. No se puede reducir total de becas si ya hay más asignados
3. Genera log de cambios

---

### RF-028 | Listar Ubicaciones
**Subsistema:** UBIC
**Prioridad:** Media
**Roles:** ADMIN, SUPERVISOR, BECA
**Dependencias:** RF-023

**Descripción:**
Usuarios pueden consultar ubicaciones según su rol.

**Criterios de Aceptación:**
1. **ADMIN:** Ve todas las ubicaciones
2. **SUPERVISOR:** Ve solo sus ubicaciones asignadas
3. **BECA:** Ve todas las ubicaciones (para información general)
4. Por cada ubicación muestra:
   - Nombre, descripción, imagen
   - Supervisor
   - Horarios
   - Becas actualmente asignados
   - Estado (Activa/Inactiva)

---

## 5.5 Sistema de Tracking de Horas

### RF-029 | Check-in: Registrar Entrada
**Subsistema:** TRACK
**Prioridad:** Alta
**Roles:** BECA
**Dependencias:** RF-020

**Descripción:**
Beca registra su entrada al iniciar jornada laboral, con evidencia fotográfica.

**Criterios de Aceptación:**
1. **Proceso:**
   - Beca abre app/sistema
   - Selecciona "Iniciar Jornada"
   - Sistema captura:
     - Ubicación (GPS - opcional)
     - Foto del beca en el lugar (cámara frontal)
     - Timestamp exacto
   - Genera registro de entrada (estado: "En Progreso")
2. **Validaciones:**
   - Beca está dentro de su horario asignado (±15 min tolerancia)
   - No tiene otra jornada activa
   - Ubicación GPS coincide con ubicación asignada (si está habilitado)
3. Notifica a supervisor de zona
4. No se puede editar después

**Notas:**
Evidencia fotográfica es obligatoria para evitar fraude.

---

### RF-030 | Check-out: Registrar Salida
**Subsistema:** TRACK
**Prioridad:** Alta
**Roles:** BECA
**Dependencias:** RF-029

**Descripción:**
Beca registra su salida al finalizar jornada laboral.

**Criterios de Aceptación:**
1. **Proceso:**
   - Beca selecciona "Finalizar Jornada"
   - Sistema captura:
     - Foto del beca (evidencia de salida)
     - Timestamp exacto
   - Calcula horas trabajadas: checkout_time - checkin_time
   - Genera registro de salida
2. **Validaciones:**
   - Tiene una jornada activa (check-in previo)
   - Tiempo mínimo trabajado: 30 minutos
   - Si excede horario asignado (ej: >4.5h), marca como "Requiere Revisión"
3. Jornada cambia a estado: "Pendiente de Aprobación"
4. Notifica a supervisor

**Notas:**
Si beca olvida hacer check-out, supervisor puede cerrarlo manualmente.

---

### RF-031 | Olvidó Hacer Check-out
**Subsistema:** TRACK
**Prioridad:** Media
**Roles:** BECA, SUPERVISOR
**Dependencias:** RF-029

**Descripción:**
Si beca olvida hacer check-out, puede solicitar corrección o supervisor puede cerrarlo.

**Criterios de Aceptación:**
1. **Opción 1 - Beca reporta:**
   - Sistema detecta jornada abierta >6 horas
   - Beca puede reportar: "Olvidé hacer check-out"
   - Especifica hora real de salida
   - Solicitud va a supervisor
2. **Opción 2 - Sistema automático:**
   - Después de 8 horas, sistema cierra jornada automáticamente
   - Marca como "Requiere Revisión - Auto-cerrado"
   - Notifica a beca y supervisor
3. **Opción 3 - Supervisor cierra:**
   - Supervisor ve jornadas abiertas de sus becas
   - Puede cerrar manualmente especificando hora de salida
4. Todas estas jornadas requieren aprobación explícita del supervisor

---

### RF-032 | Aprobar Jornada (Supervisor)
**Subsistema:** TRACK
**Prioridad:** Alta
**Roles:** SUPERVISOR
**Dependencias:** RF-030

**Descripción:**
Supervisor revisa y aprueba jornadas de trabajo de becas en su zona.

**Criterios de Aceptación:**
1. **Vista de supervisor:**
   - Lista de jornadas pendientes de aprobar
   - Por cada jornada:
     - Beca, fecha, hora entrada/salida
     - Horas trabajadas
     - Fotos de entrada/salida
     - Estado
2. **Acciones:**
   - **Aprobar:** Confirma jornada, horas se suman al total del beca
   - **Rechazar:** Especifica motivo, no se cuentan horas
   - **Ajustar:** Corrige hora entrada/salida si hay error menor
3. Supervisor puede aprobar en lote (múltiples jornadas)
4. Preferencia: aprobación dentro de 24h, pero puede ser posterior
5. Notifica a beca del resultado

**Notas:**
Flexibilidad en aprobación es importante ya que supervisor puede estar ocupado.

---

### RF-033 | Consultar Horas Acumuladas
**Subsistema:** TRACK
**Prioridad:** Media
**Roles:** BECA, SUPERVISOR, ADMIN
**Dependencias:** RF-032

**Descripción:**
Usuarios pueden consultar horas trabajadas/acumuladas.

**Criterios de Aceptación:**
1. **Vista de BECA:**
   - Horas aprobadas en semana actual
   - Horas aprobadas en mes actual
   - Total horas en el ciclo
   - Progreso hacia meta semanal/mensual
   - Horas pendientes de aprobar
   - Historial de jornadas (últimas 20)
2. **Vista de SUPERVISOR:**
   - Resumen de sus becas
   - Horas trabajadas por beca
   - Jornadas pendientes de aprobar
3. **Vista de ADMIN:**
   - Totales por ubicación
   - Totales por beca
   - Comparación contra horario asignado

---

### RF-034 | Alertas de Jornadas Irregulares
**Subsistema:** TRACK
**Prioridad:** Media
**Roles:** Sistema, SUPERVISOR
**Dependencias:** RF-030

**Descripción:**
Sistema detecta y alerta jornadas que requieren atención.

**Criterios de Aceptación:**
1. **Casos detectados:**
   - Jornada muy corta (<1h) o muy larga (>5h)
   - Check-in fuera de horario asignado
   - Múltiples check-ins en mismo día
   - Jornada abierta >6 horas
   - Check-out sin check-in
2. Marca jornada como "Requiere Revisión"
3. Notifica a supervisor
4. Supervisor debe revisar antes de aprobar/rechazar

---

## 5.6 Gestión de Ausencias

### RF-035 | Reportar Ausencia
**Subsistema:** AUS
**Prioridad:** Alta
**Roles:** BECA
**Dependencias:** RF-020

**Descripción:**
Beca puede reportar ausencias con anticipación o justificar ausencias pasadas.

**Criterios de Aceptación:**
1. **Datos de reporte:**
   - Fecha(s) de ausencia
   - Motivo (lista predefinida + "Otro")
   - Descripción/justificación
   - Documento de soporte (PDF/imagen - opcional)
2. **Tipos de ausencia:**
   - Con anticipación (≥24h): No requiere justificación si es ≤2 por mes
   - Emergencia (sin anticipación): Requiere justificación
   - Médica: Requiere documento (certificado médico)
3. Solicitud va a supervisor
4. Estado inicial: "Pendiente de Aprobación"
5. Beca recibe notificación cuando sea procesada

**Notas:**
Política: máximo 2 ausencias sin justificación por mes.

---

### RF-036 | Aprobar/Rechazar Ausencia (Supervisor)
**Subsistema:** AUS
**Prioridad:** Alta
**Roles:** SUPERVISOR
**Dependencias:** RF-035

**Descripción:**
Supervisor revisa y aprueba/rechaza reportes de ausencia.

**Criterios de Aceptación:**
1. **Vista de supervisor:**
   - Lista de ausencias pendientes
   - Por cada una: beca, fecha, motivo, documento adjunto
2. **Acciones:**
   - **Aprobar:** Ausencia justificada, no afecta evaluación
   - **Rechazar:** Especifica motivo, cuenta como falta injustificada
3. Actualiza contador de ausencias del beca
4. Notifica a beca

---

### RF-037 | Contador de Ausencias
**Subsistema:** AUS
**Prioridad:** Media
**Roles:** Sistema
**Dependencias:** RF-036

**Descripción:**
Sistema lleva conteo de ausencias por beca y genera alertas.

**Criterios de Aceptación:**
1. **Contadores:**
   - Ausencias justificadas (aprobadas)
   - Ausencias injustificadas (rechazadas o no reportadas)
   - Total de ausencias en el ciclo
2. **Alertas automáticas:**
   - ≥3 ausencias injustificadas: Alerta a supervisor y admin
   - ≥5 ausencias totales: Revisión obligatoria
   - ≥7 ausencias totales: Candidato a suspensión
3. Ausencias afectan elegibilidad para renovación

---

### RF-038 | Solicitar Adelanto de Horas
**Subsistema:** AUS
**Prioridad:** Media
**Roles:** BECA
**Dependencias:** RF-033

**Descripción:**
Beca puede solicitar trabajar horas adicionales fuera de su horario regular.

**Criterios de Aceptación:**
1. **Solicitud incluye:**
   - Fecha propuesta
   - Hora inicio y fin propuesta
   - Motivo
2. Validaciones:
   - No exceder límite semanal/mensual (ej: max 20h/semana)
   - No solapar con horario académico
   - Disponibilidad de supervisor en ese horario
3. Solicitud va a supervisor
4. Si aprueba:
   - Genera slot temporal en horario de ubicación
   - Beca puede hacer check-in/out en ese horario
5. Si rechaza:
   - Notifica a beca con motivo

---

### RF-039 | Gestionar Compensación de Horas
**Subsistema:** AUS
**Prioridad:** Baja
**Roles:** SUPERVISOR, ADMIN
**Dependencias:** RF-033

**Descripción:**
Supervisor puede ajustar horas de un beca (compensaciones, correcciones).

**Criterios de Aceptación:**
1. Supervisor puede:
   - Agregar horas manualmente (con justificación)
   - Reducir horas (ej: por sanción)
   - Corregir errores en jornadas pasadas
2. Cada ajuste requiere:
   - Motivo obligatorio
   - Genera log de auditoría
3. Beca recibe notificación del ajuste

---

## 5.7 Generación de Documentos

### RF-040 | Generar Escarapela (Carnet)
**Subsistema:** DOC
**Prioridad:** Alta
**Roles:** ADMIN
**Dependencias:** RF-020

**Descripción:**
Sistema genera escarapelas (carnets) imprimibles para becas seleccionados.

**Criterios de Aceptación:**
1. **Contenido de escarapela:**
   - Logo de universidad
   - Foto del beca
   - Nombre completo
   - Código de estudiante
   - Ubicación asignada
   - Vigencia (semestre)
   - Código QR (opcional: para validación rápida)
2. Formato: PDF imprimible (tamaño carnet estándar)
3. Admin puede:
   - Generar individual
   - Generar en lote para todos los seleccionados
4. Escarapela se guarda en registro del beca

**Notas:**
Diseño debe ser profesional y fácil de imprimir.

---

### RF-041 | Generar Bitácora Oficial
**Subsistema:** DOC
**Prioridad:** Alta
**Roles:** ADMIN, SUPERVISOR
**Dependencias:** RF-032, RF-011

**Descripción:**
Sistema genera bitácora oficial en formato PDF con todas las jornadas trabajadas del ciclo.

**Criterios de Aceptación:**
1. **Contenido de bitácora:**
   - Header: Logo universidad, datos del beca, ciclo, ubicación
   - Tabla con todas las jornadas:
     - Fecha, hora entrada, hora salida, horas trabajadas
     - Firma digital del supervisor (campo)
   - Resumen:
     - Total de horas trabajadas
     - Total de ausencias
     - Evaluación (si aplica)
   - Footer: Firmas del beca, supervisor y coordinador
2. Formato: PDF oficial (formato institucional)
3. Se genera al:
   - Solicitud del beca (borrador, no oficial)
   - Cierre del ciclo (versión final oficial)
4. Solo versión final tiene validez oficial
5. Incluye todas las jornadas aprobadas

**Notas:**
Este documento es requerido por universidad para validar horas de beca.

---

### RF-042 | Exportar Reportes
**Subsistema:** DOC
**Prioridad:** Media
**Roles:** ADMIN
**Dependencias:** RF-033

**Descripción:**
Admin puede exportar reportes en diferentes formatos.

**Criterios de Aceptación:**
1. **Tipos de reporte:**
   - Lista de becas activos (Excel/CSV)
   - Horas trabajadas por beca (Excel/CSV)
   - Ausencias por beca (Excel/CSV)
   - Resumen del ciclo (PDF)
   - Historial de jornadas (Excel/CSV)
2. Formatos soportados: PDF, Excel, CSV
3. Permite filtrar por:
   - Rango de fechas
   - Ubicación
   - Beca específico
   - Estado (activo, inactivo)

---

## 5.8 Sistema de Notificaciones

### RF-043 | Envío de Emails
**Subsistema:** NOTIF
**Prioridad:** Alta
**Roles:** Sistema
**Dependencias:** Múltiples

**Descripción:**
Sistema envía notificaciones por email a usuarios en eventos clave.

**Criterios de Aceptación:**
1. **Eventos que generan email:**
   - Usuario creado (credenciales)
   - Postulante seleccionado/rechazado
   - Jornada aprobada/rechazada
   - Ausencia aprobada/rechazada
   - Solicitud de adelanto aprobada/rechazada
   - Cambio en horario/ubicación
   - Recordatorio de fechas límite
   - Alerta de ausencias excesivas
2. Emails tienen template profesional con logo institucional
3. Incluyen enlaces directos al sistema
4. Sistema reintentos si falla envío (max 3 intentos)
5. Admin puede ver log de emails enviados

**Notas:**
Usar servicio gratuito: SendGrid (100/día) o Resend.

---

### RF-044 | Notificaciones In-App
**Subsistema:** NOTIF
**Prioridad:** Media
**Roles:** Todos
**Dependencias:** RF-001

**Descripción:**
Usuarios reciben notificaciones dentro de la aplicación.

**Criterios de Aceptación:**
1. Campanita/icono de notificaciones
2. Muestra:
   - Cantidad de notificaciones no leídas
   - Lista de notificaciones recientes
   - Título, descripción, timestamp
   - Estado (leída/no leída)
3. Click en notificación marca como leída y navega a sección relevante
4. Tipos de notificación:
   - Info (general)
   - Acción requerida (requiere respuesta)
   - Alerta (importante)

---

### RF-045 | Recordatorios Automáticos
**Subsistema:** NOTIF
**Prioridad:** Baja
**Roles:** Sistema
**Dependencias:** RF-043

**Descripción:**
Sistema envía recordatorios automáticos para fechas importantes.

**Criterios de Aceptación:**
1. **Recordatorios configurados:**
   - Postulante: 3 días antes de cierre de formulario
   - Beca: día anterior si tiene jornada asignada
   - Supervisor: cada lunes con jornadas pendientes de aprobar
   - Admin: 1 semana antes de fin de ciclo
2. Frecuencia configurable por admin
3. Usuario puede desuscribirse de ciertos recordatorios

---

## 5.9 Reportes y Consultas

### RF-046 | Dashboard Administrativo
**Subsistema:** REP
**Prioridad:** Media
**Roles:** ADMIN
**Dependencias:** Múltiples

**Descripción:**
Admin tiene dashboard con métricas clave del ciclo actual.

**Criterios de Aceptación:**
1. **Métricas mostradas:**
   - Total de becas activos
   - Postulantes (en proceso, aprobados, rechazados)
   - Horas trabajadas este mes
   - Ausencias este mes
   - Jornadas pendientes de aprobar
   - Ubicaciones con más/menos becas
   - Tasa de asistencia promedio
2. Gráficos:
   - Horas trabajadas por semana
   - Distribución de becas por ubicación
   - Tendencia de ausencias
3. Filtros por: ubicación, rango de fechas

---

### RF-047 | Dashboard de Supervisor
**Subsistema:** REP
**Prioridad:** Media
**Roles:** SUPERVISOR
**Dependencias:** Múltiples

**Descripción:**
Supervisor tiene vista de sus ubicaciones y becas asignados.

**Criterios de Aceptación:**
1. **Métricas mostradas:**
   - Becas bajo su supervisión
   - Jornadas pendientes de aprobar (destacado)
   - Ausencias reportadas pendientes
   - Horas trabajadas esta semana (por beca)
   - Alertas (becas con ausencias excesivas)
2. Accesos rápidos:
   - Aprobar jornadas
   - Revisar ausencias
   - Ver horarios de la semana

---

### RF-048 | Dashboard de Beca
**Subsistema:** REP
**Prioridad:** Media
**Roles:** BECA
**Dependencias:** Múltiples

**Descripción:**
Beca tiene vista de su progreso y estado.

**Criterios de Aceptación:**
1. **Información mostrada:**
   - Ubicación y supervisor asignado
   - Horario de la semana
   - Horas trabajadas este mes
   - Progreso hacia meta mensual (ej: 40h)
   - Próximas jornadas programadas
   - Ausencias en el ciclo
2. Acciones rápidas:
   - Iniciar jornada
   - Reportar ausencia
   - Solicitar adelanto de horas

---

### RF-049 | Consultar Postulantes
**Subsistema:** REP
**Prioridad:** Media
**Roles:** ADMIN
**Dependencias:** RF-015

**Descripción:**
Admin puede consultar y filtrar lista de postulantes.

**Criterios de Aceptación:**
1. Vista de tabla con postulantes
2. Columnas: código, nombre, email, promedio, estado, ubicación sugerida (compatibilidad)
3. Filtros:
   - Estado (pendiente, completo, entrevista, aprobado, rechazado)
   - Promedio académico (rango)
   - Ubicación compatible
4. Ordenar por: promedio, fecha de postulación, compatibilidad
5. Exportar lista filtrada

---

### RF-050 | Consultar Becas Activos
**Subsistema:** REP
**Prioridad:** Media
**Roles:** ADMIN, SUPERVISOR
**Dependencias:** RF-020

**Descripción:**
Usuarios autorizados pueden consultar becas activos en el ciclo.

**Criterios de Aceptación:**
1. Vista de tabla con becas
2. Columnas: código, nombre, ubicación, supervisor, horas trabajadas, ausencias, estado
3. Filtros:
   - Ubicación
   - Estado (activo, suspendido)
   - Supervisor
4. Click en beca → detalle completo:
   - Datos personales
   - Horario asignado
   - Historial de jornadas
   - Ausencias
   - Evaluación

---

### RF-051 | Historial de Ciclos
**Subsistema:** REP
**Prioridad:** Baja
**Roles:** ADMIN
**Dependencias:** RF-012

**Descripción:**
Admin puede consultar información de ciclos pasados.

**Criterios de Aceptación:**
1. Lista de todos los ciclos (ordenados por fecha, descendente)
2. Por ciclo muestra: nombre, fechas, total becas, estado
3. Click en ciclo → detalle:
   - Lista de becas que participaron
   - Documentos generados (bitácoras)
   - Métricas del ciclo
   - Reportes

---

## 5.10 Historial y Auditoría

### RF-052 | Log de Auditoría
**Subsistema:** HIST
**Prioridad:** Media
**Roles:** Sistema
**Dependencias:** Todos

**Descripción:**
Sistema registra todas las acciones importantes para auditoría.

**Criterios de Aceptación:**
1. **Eventos registrados:**
   - Creación/modificación/eliminación de entidades
   - Cambios de rol de usuario
   - Asignaciones de ubicaciones
   - Aprobaciones/rechazos
   - Login/logout
   - Cambio de contraseña
2. Cada log incluye:
   - Timestamp
   - Usuario que ejecutó acción
   - Acción realizada
   - Entidad afectada (tipo, ID)
   - Valores previos y nuevos (para modificaciones)
   - IP del usuario
3. Logs son inmutables (no se pueden editar/borrar)
4. Solo ADMIN puede consultar logs
5. Retención: al menos 2 años

---

### RF-053 | Historial de Beca
**Subsistema:** HIST
**Prioridad:** Media
**Roles:** ADMIN, SUPERVISOR, BECA
**Dependencias:** RF-011

**Descripción:**
Sistema mantiene historial completo de cada beca a través de múltiples ciclos.

**Criterios de Aceptación:**
1. **Historial incluye:**
   - Ciclos en los que participó
   - Ubicaciones donde trabajó
   - Supervisores que tuvo
   - Total de horas por ciclo
   - Ausencias por ciclo
   - Evaluaciones recibidas
   - Documentos generados (bitácoras)
2. **ADMIN y SUPERVISOR:** Ven historial completo
3. **BECA:** Ve solo su propio historial
4. Permite descargar documentos de ciclos pasados

---

### RF-054 | Consultar Logs de Auditoría (Admin)
**Subsistema:** HIST
**Prioridad:** Baja
**Roles:** ADMIN
**Dependencias:** RF-052

**Descripción:**
Admin puede consultar logs de auditoría para investigar incidentes.

**Criterios de Aceptación:**
1. Vista de tabla con logs
2. Columnas: timestamp, usuario, acción, entidad, detalles
3. Filtros:
   - Rango de fechas
   - Usuario
   - Tipo de acción
   - Entidad afectada
4. Búsqueda por texto
5. Exportar logs filtrados (CSV)
6. Vista de detalle expandible por log

---

## 6. Reglas de Negocio

### RN-001: Ciclo Activo Único
Solo puede haber un ciclo activo por dependencia a la vez.

### RN-002: Renovación con Prioridad
Becas anteriores con buen desempeño (≥90% asistencia, ≥95% horas) tienen prioridad en siguiente ciclo si su horario es compatible (≥70%).

### RN-003: Proceso de Renovación Simplificado
Becas que renuevan solo deben:
1. Subir horario actualizado
2. Confirmar interés
No pasan por formulario completo ni entrevista.

### RN-004: Asignación de Plazas
Plazas se asignan en este orden:
1. Renovaciones (becas anteriores compatibles)
2. Postulantes nuevos por orden de:
   - Compatibilidad de horario
   - Promedio académico
   - Resultado de entrevista

### RN-005: Límite de Becas
Suma de becas asignados a ubicaciones ≤ Total de becas del ciclo.

### RN-006: Horario de Jornada
- Jornadas típicas: 2-4 horas
- Mínimo por jornada: 30 minutos
- Máximo por jornada: 5 horas
- Máximo semanal: 20 horas

### RN-007: Ausencias
- Máximo 2 ausencias sin justificación por mes
- ≥5 ausencias totales: revisión obligatoria
- ≥7 ausencias totales: candidato a suspensión
- Ausencias afectan elegibilidad para renovación

### RN-008: Aprobación de Jornadas
- Jornadas deben ser aprobadas por supervisor
- Preferencia: aprobación dentro de 24h
- Puede ser posterior si supervisor justifica
- Solo jornadas aprobadas cuentan para total de horas

### RN-009: Check-in/Check-out
- Check-in requiere foto obligatoria (evidencia)
- Check-out también requiere foto
- Tolerancia de horario: ±15 minutos
- Si olvida check-out: debe reportar o supervisor cierra

### RN-010: Bitácora Oficial
- Se genera al cierre del ciclo
- Solo incluye jornadas aprobadas
- Requiere firma digital de supervisor
- Tiene validez oficial para universidad

### RN-011: Formato de Horario PDF
- Debe ser generado por sistema académico institucional
- Formato estándar validado por sistema
- Incluye: código estudiante, nombre, tabla de horario (L-S, 06:00-22:00)

### RN-012: Credenciales de Usuario
- Login con email institucional
- Contraseñas generadas: 12 caracteres, sin símbolos especiales (para usuarios creados masivamente)
- Usuarios de OAuth: no requieren contraseña

### RN-013: Evidencia Fotográfica
- Obligatoria en check-in y check-out
- Se almacena junto al registro de jornada
- Supervisor puede revisarla antes de aprobar

### RN-014: Multi-Dependencia
Sistema debe soportar múltiples dependencias (biblioteca, centro de cómputo, etc.), aunque actualmente solo se usa para biblioteca.

### RN-015: Cierre de Ciclo
Al cerrar ciclo:
- Todas las bitácoras deben estar generadas
- No puede haber jornadas pendientes de aprobar
- Datos quedan congelados (no editables)
- Se determina elegibilidad de renovación por beca

---

## 7. Requerimientos No Funcionales

### RNF-001: Seguridad
- Autenticación JWT
- Passwords hasheadas (bcrypt/argon2)
- HTTPS obligatorio en producción
- CORS configurado para frontend específico
- Rate limiting en endpoints sensibles
- Validación de inputs en backend

### RNF-002: Performance
- Tiempos de respuesta:
  - Endpoints de consulta: <500ms
  - Upload de archivos: <3s
  - Generación de PDFs: <5s
- Paginación en listas >50 registros
- Caché para datos estáticos (ubicaciones, horarios)

### RNF-003: Escalabilidad
- Diseño multi-tenant (multi-dependencia)
- Base de datos normalizada
- Background jobs para tareas pesadas (procesamiento Excel, emails)

### RNF-004: Disponibilidad
- Uptime objetivo: 99% (downtime permitido: ~7h/mes)
- Backups diarios de base de datos
- Logs centralizados

### RNF-005: Usabilidad
- Interfaz responsive (móvil y desktop)
- Soporte para cámara de dispositivo (check-in/out)
- Mensajes de error claros
- Confirmaciones antes de acciones destructivas

### RNF-006: Mantenibilidad
- Clean Architecture (.NET)
- Código documentado
- Tests unitarios (cobertura >70%)
- Tests de integración para flujos críticos
- CI/CD pipeline (GitHub Actions)

### RNF-007: Almacenamiento
- Archivos (fotos, PDFs, documentos):
  - Storage: AWS S3, Cloudflare R2 (gratuito hasta 10GB)
  - Límites: fotos <2MB, PDFs <5MB
- Base de datos: PostgreSQL

### RNF-008: Logging y Monitoreo
- Logs estructurados (Serilog)
- Niveles: Debug, Info, Warning, Error
- Monitoreo de errores (opcional: Sentry free tier)

---

## 8. Modelo de Datos

### Principales Entidades

#### Usuario
```
- Id (PK)
- Email (unique)
- Password (nullable si OAuth)
- Nombre
- Apellido
- Rol (ADMIN, SUPERVISOR, BECA)
- AuthProvider (Local, Google)
- Activo
- FechaCreacion
- UltimoLogin
```

#### Ciclo
```
- Id (PK)
- Nombre
- Dependencia
- FechaInicio
- FechaFin
- FechaLimitePostulaciones
- FechaEntrevistas
- FechaSeleccion
- TotalBecasDisponibles
- Estado (Configuracion, PostulacionesAbiertas, Activo, Cerrado)
- Activo (boolean - solo uno activo por dependencia)
```

#### Ubicacion
```
- Id (PK)
- Nombre
- Descripcion
- Dependencia
- TotalBecas
- Imagen (URL)
- TipoHorario (UnificadoSinSabado, UnificadoConSabado, Personalizado)
- Activa
```

#### HorarioUbicacion
```
- Id (PK)
- UbicacionId (FK)
- Dias (array o string separado por comas)
- HoraInicio
- HoraFin
- BecasRequeridos (int o JSON para personalizado)
```

#### AsignacionSupervisor
```
- Id (PK)
- SupervisorId (FK → Usuario)
- UbicacionId (FK)
- CicloId (FK)
- FechaAsignacion
```

#### Postulacion
```
- Id (PK)
- UsuarioId (FK)
- CicloId (FK)
- CodigoEstudiante
- Carrera
- Promedio
- FotoURL
- HorarioPdfURL
- HorarioJSON (horario parseado)
- FechaNacimiento
- Direccion
- Genero
- EstudiosAdicionales
- Motivacion
- Estado (PendienteFormulario, Completo, Entrevista, Aprobado, Rechazado, Cancelado)
- FechaPostulacion
- FechaEntrevista
- NotasEntrevista
- CalificacionEntrevista
```

#### CompatibilidadUbicacion
```
- Id (PK)
- PostulacionId (FK)
- UbicacionId (FK)
- PorcentajeCompatibilidad
```

#### BecaTrabajo
```
- Id (PK)
- UsuarioId (FK)
- CicloId (FK)
- UbicacionId (FK)
- PostulacionId (FK - si viene de postulación)
- EsRenovacion (boolean)
- Estado (Activo, Suspendido, Finalizado)
- FechaInicio
- FechaFin
- TotalHorasTrabajadas
- TotalAusencias
- ElegibleRenovacion (boolean)
```

#### HorarioBeca
```
- Id (PK)
- BecaTrabajoId (FK)
- HorarioUbicacionId (FK)
- Dias
- HoraInicio
- HoraFin
```

#### Jornada
```
- Id (PK)
- BecaTrabajoId (FK)
- Fecha
- CheckInTimestamp
- CheckInFotoURL
- CheckInGPS (opcional)
- CheckOutTimestamp
- CheckOutFotoURL
- HorasTrabajadas (calculado)
- Estado (EnProgreso, PendienteAprobacion, Aprobada, Rechazada, RequiereRevision)
- SupervisorAprobadorId (FK → Usuario)
- FechaAprobacion
- MotivoRechazo
- Observaciones
```

#### Ausencia
```
- Id (PK)
- BecaTrabajoId (FK)
- Fecha
- Motivo
- Descripcion
- DocumentoSoporteURL (nullable)
- Tipo (ConAnticipacion, Emergencia, Medica)
- Estado (Pendiente, Aprobada, Rechazada)
- SupervisorRevisorId (FK)
- FechaRevision
- MotivoRechazo
```

#### SolicitudAdelantoHoras
```
- Id (PK)
- BecaTrabajoId (FK)
- FechaPropuesta
- HoraInicioPropuesta
- HoraFinPropuesta
- Motivo
- Estado (Pendiente, Aprobada, Rechazada)
- SupervisorRevisorId (FK)
- FechaRevision
- MotivoRechazo
```

#### Notificacion
```
- Id (PK)
- UsuarioId (FK)
- Tipo (Info, AccionRequerida, Alerta)
- Titulo
- Descripcion
- Leida
- FechaCreacion
- EnlaceAccion (nullable)
```

#### LogAuditoria
```
- Id (PK)
- UsuarioId (FK)
- Accion
- EntidadTipo
- EntidadId
- ValorAnterior (JSON)
- ValorNuevo (JSON)
- IP
- Timestamp
```

---

## 9. Flujos de Procesos

### Flujo 1: Creación de Nuevo Ciclo

```
1. [ADMIN] Crear nuevo ciclo → RF-006
2. [ADMIN] Configurar ubicaciones y horarios → RF-007, RF-023, RF-025
3. [ADMIN] Asignar supervisores a ubicaciones → RF-026
4. [ADMIN] Abrir periodo de postulaciones → RF-008
5. [Sistema] Notifica a potenciales postulantes → RF-043
```

### Flujo 2: Postulación de Nuevo Beca

```
1. [ADMIN] Sube lista de postulantes (Excel) → RF-013
2. [Sistema] Muestra preview, admin confirma → RF-014
3. [Sistema] Crea usuarios, envía credenciales → RF-014, RF-043
4. [POSTULANTE] Recibe email, hace login → RF-001
5. [POSTULANTE] Completa formulario (foto, horario PDF) → RF-015
6. [Sistema] Valida PDF → RF-016
7. [Sistema] Calcula compatibilidad con ubicaciones → RF-017
8. [Sistema] Cambia estado a "Formulario Completado"
9. [ADMIN] Cierra periodo de postulaciones → RF-009
```

### Flujo 3: Selección Final

```
1. [ADMIN] Revisa lista de postulantes completos → RF-049
2. [ADMIN] Programa entrevistas → RF-018
3. [ADMIN/SUPERVISOR] Realiza entrevistas, registra resultados → RF-018
4. [ADMIN] Asigna postulantes aprobados a ubicaciones → RF-019
5. [ADMIN] Confirma selección final → RF-020
6. [Sistema] Cambia rol de seleccionados: POSTULANTE → BECA
7. [Sistema] Notifica a todos (aprobados y rechazados) → RF-043
8. [Sistema] Genera escarapelas → RF-040
9. [ADMIN] Cambia estado del ciclo a "Activo"
```

### Flujo 4: Renovación de Beca Anterior

```
1. [ADMIN] Inicia nuevo ciclo → RF-006
2. [Sistema] Identifica becas elegibles para renovación → RF-021
3. [Sistema] Envía invitaciones de renovación → RF-043
4. [BECA] Recibe invitación, sube horario actualizado → RF-021
5. [Sistema] Valida PDF → RF-016
6. [Sistema] Calcula compatibilidad con ubicación anterior → RF-017
7. [Sistema - Si compatible ≥70%] Asignación automática a misma ubicación
8. [Sistema - Si no compatible] Pasa a pool de postulantes normales
9. [ADMIN] Revisa renovaciones, confirma → RF-020
10. [Sistema] Asignación de plazas restantes a postulantes nuevos
```

### Flujo 5: Jornada Laboral Normal

```
1. [BECA] Llega a ubicación, abre app → RF-048
2. [BECA] Click "Iniciar Jornada" → RF-029
3. [Sistema] Solicita foto (cámara frontal)
4. [BECA] Toma foto, confirma check-in
5. [Sistema] Registra: timestamp, foto, GPS (opcional)
6. [Sistema] Notifica a supervisor → RF-044
--- Beca trabaja 2-4 horas ---
7. [BECA] Click "Finalizar Jornada" → RF-030
8. [Sistema] Solicita foto de salida
9. [BECA] Toma foto, confirma check-out
10. [Sistema] Calcula horas trabajadas
11. [Sistema] Cambia estado jornada a "Pendiente Aprobación"
12. [Sistema] Notifica a supervisor → RF-044
13. [SUPERVISOR] Revisa jornada (fotos, horas) → RF-032
14. [SUPERVISOR] Aprueba jornada
15. [Sistema] Suma horas al total del beca
16. [Sistema] Notifica a beca → RF-043
```

### Flujo 6: Beca Olvidó Check-out

```
1. [Sistema] Detecta jornada abierta >6 horas → RF-031
2. [Sistema] Notifica a beca y supervisor → RF-044
3. [BECA - Opción 1] Reporta "Olvidé check-out", especifica hora real
4. [SUPERVISOR] Revisa solicitud, ajusta hora de salida
5. [SUPERVISOR] Aprueba con ajuste → RF-032
--- O ---
3. [Sistema - Opción 2] Después de 8h, auto-cierra jornada
4. [Sistema] Marca como "Requiere Revisión - Auto-cerrado"
5. [SUPERVISOR] Revisa, ajusta/aprueba/rechaza → RF-032
```

### Flujo 7: Reportar Ausencia

```
1. [BECA] Sabe que no podrá asistir → RF-035
2. [BECA] Reporta ausencia: fecha, motivo, documento (si aplica)
3. [Sistema] Crea solicitud, estado "Pendiente"
4. [Sistema] Notifica a supervisor → RF-044
5. [SUPERVISOR] Revisa solicitud → RF-036
6. [SUPERVISOR] Aprueba o rechaza (con motivo)
7. [Sistema] Actualiza contador de ausencias → RF-037
8. [Sistema] Notifica a beca → RF-043
9. [Sistema - Si ≥3 injustificadas] Alerta a admin → RF-037
```

### Flujo 8: Cierre de Ciclo

```
1. [Sistema] Fecha fin del ciclo se aproxima → RF-045
2. [Sistema] Envía recordatorios a supervisores → RF-045
3. [SUPERVISOR] Aprueba jornadas pendientes → RF-032
4. [ADMIN] Verifica que todo esté completo → RF-011
5. [ADMIN] Cierra ciclo oficialmente → RF-011
6. [Sistema] Genera bitácoras oficiales para todos los becas → RF-041
7. [Sistema] Marca becas elegibles para renovación → RF-011
8. [Sistema] Congela datos del ciclo (no editables)
9. [Sistema] Cambia estado del ciclo a "Cerrado"
10. [Sistema] Genera reporte final del ciclo → RF-042
```

---

## 10. Tabla Resumen de Todos los Requerimientos Funcionales

| Código | Nombre | Subsistema | Prioridad | Roles |
|--------|--------|------------|-----------|-------|
| **RF-001** | Login con Email y Contraseña | AUTH | Alta | Todos |
| **RF-002** | Login con OAuth (Google) | AUTH | Media | BECA, POSTULANTE |
| **RF-003** | Gestión de Roles y Permisos | AUTH | Alta | ADMIN |
| **RF-004** | Recuperación de Contraseña | AUTH | Media | Todos |
| **RF-005** | Cambio de Contraseña (Usuario Autenticado) | AUTH | Baja | Todos |
| **RF-006** | Crear Nuevo Ciclo Semestral | CICLO | Alta | ADMIN |
| **RF-007** | Configurar Ciclo | CICLO | Alta | ADMIN |
| **RF-008** | Abrir Periodo de Postulaciones | CICLO | Alta | ADMIN |
| **RF-009** | Cerrar Periodo de Postulaciones | CICLO | Alta | ADMIN |
| **RF-010** | Extender Fechas del Ciclo | CICLO | Media | ADMIN |
| **RF-011** | Cerrar Ciclo Semestral | CICLO | Alta | ADMIN |
| **RF-012** | Ver Historial de Ciclos | CICLO | Media | ADMIN |
| **RF-013** | Subir Lista de Postulantes (Excel) | SEL | Alta | ADMIN |
| **RF-014** | Confirmar Lista y Crear Usuarios Postulantes | SEL | Alta | ADMIN |
| **RF-015** | Postulante Completa Formulario | SEL | Alta | BECA (postulante) |
| **RF-016** | Validar Formato de Horario PDF | SEL | Alta | Sistema (backend) |
| **RF-017** | Matching Automático Postulante-Ubicación | SEL | Alta | Sistema (backend) |
| **RF-018** | Gestionar Proceso de Entrevistas | SEL | Media | ADMIN, SUPERVISOR |
| **RF-019** | Asignar Postulantes a Ubicaciones | SEL | Alta | ADMIN |
| **RF-020** | Confirmar Selección Final | SEL | Alta | ADMIN |
| **RF-021** | Proceso de Renovación (Becas Anteriores) | SEL | Alta | ADMIN, BECA |
| **RF-022** | Rechazar/Cancelar Postulación | SEL | Baja | ADMIN, BECA (postulante) |
| **RF-023** | Crear Ubicación | UBIC | Alta | ADMIN |
| **RF-024** | Tipos de Horario de Ubicación | UBIC | Alta | Sistema |
| **RF-025** | Definir Horarios de Ubicación | UBIC | Alta | ADMIN |
| **RF-026** | Asignar Supervisor a Ubicación | UBIC | Alta | ADMIN |
| **RF-027** | Actualizar Ubicación | UBIC | Media | ADMIN |
| **RF-028** | Listar Ubicaciones | UBIC | Media | ADMIN, SUPERVISOR, BECA |
| **RF-029** | Check-in: Registrar Entrada | TRACK | Alta | BECA |
| **RF-030** | Check-out: Registrar Salida | TRACK | Alta | BECA |
| **RF-031** | Olvidó Hacer Check-out | TRACK | Media | BECA, SUPERVISOR |
| **RF-032** | Aprobar Jornada (Supervisor) | TRACK | Alta | SUPERVISOR |
| **RF-033** | Consultar Horas Acumuladas | TRACK | Media | BECA, SUPERVISOR, ADMIN |
| **RF-034** | Alertas de Jornadas Irregulares | TRACK | Media | Sistema, SUPERVISOR |
| **RF-035** | Reportar Ausencia | AUS | Alta | BECA |
| **RF-036** | Aprobar/Rechazar Ausencia (Supervisor) | AUS | Alta | SUPERVISOR |
| **RF-037** | Contador de Ausencias | AUS | Media | Sistema |
| **RF-038** | Solicitar Adelanto de Horas | AUS | Media | BECA |
| **RF-039** | Gestionar Compensación de Horas | AUS | Baja | SUPERVISOR, ADMIN |
| **RF-040** | Generar Escarapela (Carnet) | DOC | Alta | ADMIN |
| **RF-041** | Generar Bitácora Oficial | DOC | Alta | ADMIN, SUPERVISOR |
| **RF-042** | Exportar Reportes | DOC | Media | ADMIN |
| **RF-043** | Envío de Emails | NOTIF | Alta | Sistema |
| **RF-044** | Notificaciones In-App | NOTIF | Media | Todos |
| **RF-045** | Recordatorios Automáticos | NOTIF | Baja | Sistema |
| **RF-046** | Dashboard Administrativo | REP | Media | ADMIN |
| **RF-047** | Dashboard de Supervisor | REP | Media | SUPERVISOR |
| **RF-048** | Dashboard de Beca | REP | Media | BECA |
| **RF-049** | Consultar Postulantes | REP | Media | ADMIN |
| **RF-050** | Consultar Becas Activos | REP | Media | ADMIN, SUPERVISOR |
| **RF-051** | Historial de Ciclos | REP | Baja | ADMIN |
| **RF-052** | Log de Auditoría | HIST | Media | Sistema |
| **RF-053** | Historial de Beca | HIST | Media | ADMIN, SUPERVISOR, BECA |
| **RF-054** | Consultar Logs de Auditoría (Admin) | HIST | Baja | ADMIN |

### Resumen por Subsistema

| Subsistema | Código | Total RFs | Alta | Media | Baja |
|------------|--------|-----------|------|-------|------|
| **Autenticación y Autorización** | AUTH | 5 | 2 | 2 | 1 |
| **Gestión de Ciclos/Semestres** | CICLO | 7 | 5 | 2 | 0 |
| **Proceso de Selección** | SEL | 10 | 7 | 2 | 1 |
| **Gestión de Ubicaciones** | UBIC | 6 | 4 | 2 | 0 |
| **Sistema de Tracking de Horas** | TRACK | 6 | 3 | 3 | 0 |
| **Gestión de Ausencias** | AUS | 5 | 2 | 2 | 1 |
| **Generación de Documentos** | DOC | 3 | 2 | 1 | 0 |
| **Sistema de Notificaciones** | NOTIF | 3 | 1 | 1 | 1 |
| **Reportes y Consultas** | REP | 6 | 0 | 5 | 1 |
| **Historial y Auditoría** | HIST | 3 | 0 | 2 | 1 |
| **TOTAL** | | **54** | **26** | **22** | **6** |

### Distribución por Prioridad

- 🔴 **Alta Prioridad:** 26 RFs (48%) - MVP y Core
- 🟡 **Media Prioridad:** 22 RFs (41%) - Mejoras
- 🟢 **Baja Prioridad:** 6 RFs (11%) - Refinamiento

---

## Conclusión

Este documento define **54 requerimientos funcionales** organizados en **10 subsistemas** para el Sistema de Gestión y Seguimiento de Becas Trabajo.

### Prioridades para Implementación

**Fase 1 - MVP (Alta Prioridad):**
- Subsistema AUTH (RF-001 a RF-005)
- Subsistema CICLO (RF-006 a RF-011)
- Subsistema SEL - básico (RF-013 a RF-020)
- Subsistema UBIC (RF-023 a RF-028)

**Fase 2 - Core (Alta Prioridad):**
- Subsistema TRACK (RF-029 a RF-034)
- Subsistema AUS (RF-035 a RF-039)
- Subsistema DOC (RF-040 a RF-042)
- Subsistema NOTIF - emails (RF-043)

**Fase 3 - Mejoras (Media Prioridad):**
- Subsistema SEL - renovación (RF-021)
- Subsistema REP (RF-046 a RF-051)
- Subsistema NOTIF - in-app (RF-044, RF-045)
- Subsistema HIST (RF-052 a RF-054)

**Fase 4 - Refinamiento (Baja Prioridad):**
- RF-022, RF-027, RF-039, RF-045, RF-051

---

**Próximos Pasos:**
1. Revisar y aprobar requerimientos
2. Diseñar arquitectura .NET (Clean Architecture)
3. Definir tecnologías específicas
4. Crear backlog de desarrollo
5. Comenzar implementación por fases
