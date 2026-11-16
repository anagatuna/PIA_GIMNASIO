-- =============================================================
-- LIMPIEZA DE DATOS EXISTENTES (correr una sola vez, despues de Correcciones.sql)
-- Deja los registros que ya existian igual que como los guardan ahora los SP:
-- sin espacios de mas, en mayusculas y sin acentos (dbo.fnRemoverTildes).
-- Solo toca los mismos campos que limpian los SP (no correos, cuentas,
-- contraseñas ni campos que se comparan contra listas fijas del codigo).
-- =============================================================
USE [DBGIMNASIO]
GO

BEGIN TRY
	BEGIN TRANSACTION

	-- Socios
	UPDATE CLIENTE
	SET Nombre			= dbo.fnRemoverTildes(LTRIM(RTRIM(Nombre))),
		PrimerApellido	= dbo.fnRemoverTildes(LTRIM(RTRIM(PrimerApellido))),
		SegundoApellido	= dbo.fnRemoverTildes(LTRIM(RTRIM(SegundoApellido)))

	-- Expediente medico
	UPDATE EXPEDIENTE_MEDICO
	SET PadecimientosCronicos	= dbo.fnRemoverTildes(LTRIM(RTRIM(PadecimientosCronicos))),
		LesionesPrevias			= dbo.fnRemoverTildes(LTRIM(RTRIM(LesionesPrevias))),
		Alergias				= dbo.fnRemoverTildes(LTRIM(RTRIM(Alergias))),
		CondicionRiesgo			= dbo.fnRemoverTildes(LTRIM(RTRIM(CondicionRiesgo)))

	-- Empleados
	UPDATE EMPLEADO
	SET Nombre			= dbo.fnRemoverTildes(LTRIM(RTRIM(Nombre))),
		PrimerApellido	= dbo.fnRemoverTildes(LTRIM(RTRIM(PrimerApellido))),
		SegundoApellido	= dbo.fnRemoverTildes(LTRIM(RTRIM(SegundoApellido))),
		CURP			= dbo.fnRemoverTildes(LTRIM(RTRIM(CURP))),
		RFC				= dbo.fnRemoverTildes(LTRIM(RTRIM(RFC)))

	-- Sucursales
	UPDATE SUCURSAL
	SET Nombre		= dbo.fnRemoverTildes(LTRIM(RTRIM(Nombre))),
		Calle		= dbo.fnRemoverTildes(LTRIM(RTRIM(Calle))),
		NumeroInt	= dbo.fnRemoverTildes(LTRIM(RTRIM(NumeroInt))),
		Colonia		= dbo.fnRemoverTildes(LTRIM(RTRIM(Colonia))),
		Municipio	= dbo.fnRemoverTildes(LTRIM(RTRIM(Municipio)))

	-- Clases
	UPDATE CLASE
	SET Nombre	= dbo.fnRemoverTildes(LTRIM(RTRIM(Nombre))),
		Salon	= dbo.fnRemoverTildes(LTRIM(RTRIM(Salon)))

	-- Rutinas (solo el nombre; el objetivo se compara contra la lista del sistema)
	UPDATE RUTINA
	SET Nombre = dbo.fnRemoverTildes(LTRIM(RTRIM(Nombre)))

	COMMIT TRANSACTION
	PRINT 'Limpieza terminada.'
END TRY
BEGIN CATCH
	IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION
	PRINT 'No se pudo limpiar: ' + ERROR_MESSAGE()
END CATCH
GO
