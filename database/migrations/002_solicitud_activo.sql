-- ============================================================
-- Migración 002 — tabla SolicitudActivo (solicitudes de inscripción de activos,
-- con propuesta de categoría / encargado nuevos)
--
-- Cubre dos casos:
--   A) La tabla no existe todavía  -> la crea con la forma actual.
--   B) La tabla existe con la forma vieja (CategoriaId / EncargadoId NOT NULL,
--      sin columnas de "propuesta de creación") -> la actualiza:
--        * agrega CategoriaNuevaNombre/Icono y EncargadoNuevoNombre/Rol
--        * vuelve CategoriaId / EncargadoId anulables (suelta y recrea sus FK)
--        * agrega los CHECK "una sola vía" para categoría y encargado
--
-- Idempotente: correrla más de una vez no hace daño. Cada paso va en su propio
-- lote (GO) porque SQL Server no puede referenciar en un CHECK una columna
-- agregada en el mismo lote.
--
-- La aplica automáticamente database/init.sh (contenedor db). Manual:
--   sqlcmd -S <servidor> -d SIBI -i database/migrations/002_solicitud_activo.sql
-- (En instalaciones nuevas la tabla ya viene completa en SIBI.sql.)
-- ============================================================
USE SIBI;
GO

-- ── Caso A: crear desde cero ────────────────────────────────
IF OBJECT_ID('dbo.SolicitudActivo', 'U') IS NULL
    CREATE TABLE SolicitudActivo (
        Id                INT           IDENTITY(1,1) NOT NULL,
        SolicitanteCorreo VARCHAR(50)   NOT NULL,
        FechaSolicitud    DATETIME      NOT NULL DEFAULT GETDATE(),

        Placa             VARCHAR(10)   NOT NULL,
        TipoPlaca         VARCHAR(15)   NOT NULL DEFAULT 'Institucional',
        Marca             VARCHAR(50)   NOT NULL,
        Modelo            VARCHAR(50)   NOT NULL,
        NumSerial         VARCHAR(50)   NOT NULL,
        Articulo          VARCHAR(50)   NOT NULL,
        Observaciones     VARCHAR(200)  NULL,
        UbicacionActual   VARCHAR(60)   NOT NULL,

        CategoriaId          INT           NULL,
        CategoriaNuevaNombre NVARCHAR(30)  NULL,
        CategoriaNuevaIcono  NVARCHAR(30)  NULL,

        EncargadoId          UNIQUEIDENTIFIER NULL,
        EncargadoNuevoNombre VARCHAR(50)   NULL,
        EncargadoNuevoRol    VARCHAR(30)   NULL,

        Estado            VARCHAR(20)   NOT NULL DEFAULT 'Pendiente',
        RevisorCorreo     VARCHAR(50)   NULL,
        FechaResolucion   DATETIME      NULL,
        Comentario        NVARCHAR(500) NULL,
        PlacaCreada       VARCHAR(10)   NULL,

        CONSTRAINT PK_SolicitudActivo PRIMARY KEY (Id),
        CONSTRAINT FK_SolicitudActivo_Solicitante
            FOREIGN KEY (SolicitanteCorreo) REFERENCES Usuario(Correo),
        CONSTRAINT FK_SolicitudActivo_Revisor
            FOREIGN KEY (RevisorCorreo)     REFERENCES Usuario(Correo),
        CONSTRAINT FK_SolicitudActivo_Categoria
            FOREIGN KEY (CategoriaId)       REFERENCES Categoria(Id),
        CONSTRAINT FK_SolicitudActivo_Encargado
            FOREIGN KEY (EncargadoId)       REFERENCES Encargado(Id),
        CONSTRAINT CHK_SolicitudActivo_Estado
            CHECK (Estado IN ('Pendiente', 'Aprobada', 'Rechazada')),
        CONSTRAINT CHK_SolicitudActivo_TipoPlaca
            CHECK (TipoPlaca IN ('Institucional', 'Interno')),
        CONSTRAINT CHK_SolicitudActivo_Categoria
            CHECK ((CASE WHEN CategoriaId IS NULL THEN 0 ELSE 1 END)
                 + (CASE WHEN CategoriaNuevaNombre IS NULL THEN 0 ELSE 1 END) = 1),
        CONSTRAINT CHK_SolicitudActivo_Encargado_Via
            CHECK ((CASE WHEN EncargadoId IS NULL THEN 0 ELSE 1 END)
                 + (CASE WHEN EncargadoNuevoNombre IS NULL THEN 0 ELSE 1 END) = 1)
    );
GO

-- ── Caso B: agregar columnas de propuesta (un lote por columna) ──
IF COL_LENGTH('dbo.SolicitudActivo', 'CategoriaNuevaNombre') IS NULL
    ALTER TABLE SolicitudActivo ADD CategoriaNuevaNombre NVARCHAR(30) NULL;
GO
IF COL_LENGTH('dbo.SolicitudActivo', 'CategoriaNuevaIcono') IS NULL
    ALTER TABLE SolicitudActivo ADD CategoriaNuevaIcono NVARCHAR(30) NULL;
GO
IF COL_LENGTH('dbo.SolicitudActivo', 'EncargadoNuevoNombre') IS NULL
    ALTER TABLE SolicitudActivo ADD EncargadoNuevoNombre VARCHAR(50) NULL;
GO
IF COL_LENGTH('dbo.SolicitudActivo', 'EncargadoNuevoRol') IS NULL
    ALTER TABLE SolicitudActivo ADD EncargadoNuevoRol VARCHAR(30) NULL;
GO

-- ── Caso B: volver CategoriaId / EncargadoId anulables ──────
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('dbo.SolicitudActivo')
             AND name = 'CategoriaId' AND is_nullable = 0)
BEGIN
    IF OBJECT_ID('FK_SolicitudActivo_Categoria', 'F') IS NOT NULL
        ALTER TABLE SolicitudActivo DROP CONSTRAINT FK_SolicitudActivo_Categoria;
    ALTER TABLE SolicitudActivo ALTER COLUMN CategoriaId INT NULL;
    ALTER TABLE SolicitudActivo ADD CONSTRAINT FK_SolicitudActivo_Categoria
        FOREIGN KEY (CategoriaId) REFERENCES Categoria(Id);
END
GO
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('dbo.SolicitudActivo')
             AND name = 'EncargadoId' AND is_nullable = 0)
BEGIN
    IF OBJECT_ID('FK_SolicitudActivo_Encargado', 'F') IS NOT NULL
        ALTER TABLE SolicitudActivo DROP CONSTRAINT FK_SolicitudActivo_Encargado;
    ALTER TABLE SolicitudActivo ALTER COLUMN EncargadoId UNIQUEIDENTIFIER NULL;
    ALTER TABLE SolicitudActivo ADD CONSTRAINT FK_SolicitudActivo_Encargado
        FOREIGN KEY (EncargadoId) REFERENCES Encargado(Id);
END
GO

-- ── Caso B: CHECK "una sola vía" (las columnas ya existen) ──
IF OBJECT_ID('CHK_SolicitudActivo_Categoria', 'C') IS NULL
    ALTER TABLE SolicitudActivo ADD CONSTRAINT CHK_SolicitudActivo_Categoria
        CHECK ((CASE WHEN CategoriaId IS NULL THEN 0 ELSE 1 END)
             + (CASE WHEN CategoriaNuevaNombre IS NULL THEN 0 ELSE 1 END) = 1);
GO
IF OBJECT_ID('CHK_SolicitudActivo_Encargado_Via', 'C') IS NULL
    ALTER TABLE SolicitudActivo ADD CONSTRAINT CHK_SolicitudActivo_Encargado_Via
        CHECK ((CASE WHEN EncargadoId IS NULL THEN 0 ELSE 1 END)
             + (CASE WHEN EncargadoNuevoNombre IS NULL THEN 0 ELSE 1 END) = 1);
GO
