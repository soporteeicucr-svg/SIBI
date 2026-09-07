-- ============================================================
-- Migración 001 — Categoria.Nombre / Icono a NVARCHAR
-- (soporte de emoji y Unicode en el ícono / nombre de categoría)
--
-- Idempotente: solo actúa si las columnas siguen siendo VARCHAR.
-- ============================================================
USE SIBI;
GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Categoria')
      AND name = 'Nombre'
      AND TYPE_NAME(system_type_id) = 'varchar'
)
BEGIN
    IF OBJECT_ID('UQ_Categoria_Nombre', 'UQ') IS NOT NULL
        ALTER TABLE Categoria DROP CONSTRAINT UQ_Categoria_Nombre;

    ALTER TABLE Categoria ALTER COLUMN Nombre NVARCHAR(30) NOT NULL;
    ALTER TABLE Categoria ALTER COLUMN Icono  NVARCHAR(30) NULL;
END
GO

IF OBJECT_ID('UQ_Categoria_Nombre', 'UQ') IS NULL
    ALTER TABLE Categoria ADD CONSTRAINT UQ_Categoria_Nombre UNIQUE (Nombre);
GO
