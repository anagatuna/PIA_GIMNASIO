-- =============================================================
-- CORRECCIONES (revision del PIA)
-- Correr este script sobre DBGIMNASIO despues del script completo.
-- Solo modifica la funcion y los stored procedures (CREATE OR ALTER); no borra datos.
-- =============================================================
USE [DBGIMNASIO]
GO

-- Pasa el texto a mayusculas y quita los acentos de las vocales (la Ñ se respeta).
-- Los SP la usan con LTRIM(RTRIM(...)) para guardar los textos siempre igual.
CREATE OR ALTER FUNCTION [dbo].[fnRemoverTildes]
(
	@Cadena NVARCHAR(MAX)
)
RETURNS NVARCHAR(MAX)
AS
BEGIN
	-- Convertir cadena a mayusculas
	SET @Cadena = UPPER(@Cadena)
	-- Reemplazar las vocales acentuadas
	RETURN REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@Cadena,
		'Á', 'A'), 'É', 'E'), 'Í', 'I'), 'Ó', 'O'), 'Ú', 'U'), 'Ü', 'U')
END
GO

CREATE OR ALTER PROCEDURE [dbo].[spSucursal]
	@Accion				INT				=	NULL,
	@IdSucursal			INT				=	NULL,
	@IdEstatus			INT				=	NULL,
	@IdEstado			INT				=	NULL,
	@Nombre				NVARCHAR(50)	=	NULL,
	@Telefono			NVARCHAR(15)	=	NULL,
	@AforoMaximo		INT				=	NULL,
	@HoraApertura		TIME			=	NULL,
	@HoraCierre			TIME			=	NULL,
	@Colonia			NVARCHAR(50)	=	NULL,
	@Calle				NVARCHAR(50)	=	NULL,
	@NumeroInt			NVARCHAR(50)	=	NULL,
	@CodigoPostal		NVARCHAR(50)	=	NULL,
	@Municipio			NVARCHAR(50)	=	NULL,
	@UsuarioSesion		INT				=	NULL
AS
BEGIN
	SET NOCOUNT ON;

	-- Textos sin acentos, en mayusculas y sin espacios de mas (dbo.fnRemoverTildes)
	SET @Nombre = dbo.fnRemoverTildes(LTRIM(RTRIM(@Nombre)))
	SET @Colonia = dbo.fnRemoverTildes(LTRIM(RTRIM(@Colonia)))
	SET @Calle = dbo.fnRemoverTildes(LTRIM(RTRIM(@Calle)))
	SET @NumeroInt = dbo.fnRemoverTildes(LTRIM(RTRIM(@NumeroInt)))
	SET @Municipio = dbo.fnRemoverTildes(LTRIM(RTRIM(@Municipio)))

	IF @Accion IN (1, 4)
	BEGIN
		IF ISNULL(@AforoMaximo, 0) <= 0 OR @AforoMaximo > 5000
		BEGIN
			SELECT 'El aforo máximo debe estar entre 1 y 5000 personas.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF @HoraCierre <= @HoraApertura
		BEGIN
			SELECT 'La hora de cierre debe ser posterior a la hora de apertura.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF EXISTS (SELECT 1 FROM SUCURSAL WHERE Nombre = @Nombre AND FechaBaja IS NULL AND IdSucursal <> ISNULL(@IdSucursal, 0))
		BEGIN
			SELECT 'Ya existe una sucursal llamada "' + @Nombre + '".' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END
	END

	-- Reglas solo al modificar: el aforo no puede quedar por debajo de lo que ya hay registrado
	-- y el horario no se cambia si la sucursal tiene clases programadas
	IF @Accion = 4
	BEGIN
		DECLARE @DentroAhora INT, @CupoMayor INT
		SELECT @DentroAhora = COUNT(*) FROM CONTROL_ACCESO
		WHERE IdSucursal = @IdSucursal AND FechaSalida IS NULL AND CAST(FechaEntrada AS DATE) = CAST(GETDATE() AS DATE)

		SELECT @CupoMayor = ISNULL(MAX(CupoMaximo), 0) FROM CLASE
		WHERE IdSucursal = @IdSucursal AND FechaBaja IS NULL AND Fecha >= CAST(GETDATE() AS DATE)

		IF @AforoMaximo < @DentroAhora
		BEGIN
			SELECT 'El aforo no puede ser menor a los ' + CAST(@DentroAhora AS NVARCHAR(10)) + ' socios que están dentro de la sucursal.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF @AforoMaximo < @CupoMayor
		BEGIN
			SELECT 'El aforo no puede ser menor a ' + CAST(@CupoMayor AS NVARCHAR(10)) + ': hay clases programadas con ese cupo.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF EXISTS (SELECT 1 FROM SUCURSAL WHERE IdSucursal = @IdSucursal AND (HoraApertura <> @HoraApertura OR HoraCierre <> @HoraCierre))
		   AND EXISTS (SELECT 1 FROM CLASE WHERE IdSucursal = @IdSucursal AND FechaBaja IS NULL AND Fecha >= CAST(GETDATE() AS DATE))
		BEGIN
			SELECT 'No se puede modificar el horario: la sucursal tiene clases programadas.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END
	END

	IF @Accion = 1
	BEGIN
		INSERT INTO SUCURSAL (IdEstatus, IdEstado, Nombre, Telefono, AforoMaximo, HoraApertura, HoraCierre,
							  Colonia, Calle, NumeroInt, CodigoPostal, Municipio, FechaRegistro, UsuarioRegistro)
		VALUES (@IdEstatus, @IdEstado, @Nombre, @Telefono, @AforoMaximo, @HoraApertura, @HoraCierre,
				@Colonia, @Calle, @NumeroInt, @CodigoPostal, @Municipio, GETDATE(), @UsuarioSesion)

		SELECT 'Sucursal registrada correctamente.' AS Mensaje, 'success' AS TipoMensaje
	END

	IF @Accion = 2
	BEGIN
		SELECT IdSucursal, IdEstatus, IdEstado, Nombre, Telefono, AforoMaximo, HoraApertura, HoraCierre,
			   Colonia, Calle, NumeroInt, CodigoPostal, Municipio
		FROM SUCURSAL
		WHERE IdSucursal = @IdSucursal AND FechaBaja IS NULL
	END

	-- CONSULTA GENERAL (incluye la ocupación actual)
	IF @Accion = 3
	BEGIN
		SELECT
			S.IdSucursal,
			S.Nombre,
			S.Telefono,
			S.AforoMaximo,
			S.HoraApertura,
			S.HoraCierre,
			S.Calle + ' ' + S.NumeroInt + ', ' + S.Colonia + ', ' + S.Municipio + ', ' + ED.Descripcion + ' C.P. ' + S.CodigoPostal AS Direccion,
			ED.Descripcion	AS Estado,
			S.IdEstatus,
			ES.Descripcion	AS Estatus,
			(SELECT COUNT(*) FROM CONTROL_ACCESO CA
			 WHERE CA.IdSucursal = S.IdSucursal AND CA.FechaSalida IS NULL
			   AND CAST(CA.FechaEntrada AS DATE) = CAST(GETDATE() AS DATE))		AS Ocupacion,
			(SELECT COUNT(*) FROM EMPLEADO E WHERE E.IdSucursal = S.IdSucursal AND E.FechaBaja IS NULL) AS TotalEmpleados
		FROM SUCURSAL S
		INNER JOIN ESTADO ED	ON ED.IdEstado = S.IdEstado
		INNER JOIN ESTATUS ES	ON ES.IdEstatus = S.IdEstatus
		WHERE S.FechaBaja IS NULL
		  AND (@Nombre IS NULL OR S.Nombre LIKE '%' + @Nombre + '%')
		  AND (@IdEstado IS NULL OR S.IdEstado = @IdEstado)
		  AND (@IdEstatus IS NULL OR S.IdEstatus = @IdEstatus)
		ORDER BY S.Nombre
	END

	IF @Accion = 4
	BEGIN
		UPDATE SUCURSAL
		SET IdEstatus			= @IdEstatus,
			IdEstado			= @IdEstado,
			Nombre				= @Nombre,
			Telefono			= @Telefono,
			AforoMaximo			= @AforoMaximo,
			HoraApertura		= @HoraApertura,
			HoraCierre			= @HoraCierre,
			Colonia				= @Colonia,
			Calle				= @Calle,
			NumeroInt			= @NumeroInt,
			CodigoPostal		= @CodigoPostal,
			Municipio			= @Municipio,
			FechaModificacion	= GETDATE(),
			UsuarioModificacion	= @UsuarioSesion
		WHERE IdSucursal = @IdSucursal AND FechaBaja IS NULL

		IF @@ROWCOUNT = 0
			SELECT 'No se encontró la sucursal a modificar.' AS Mensaje, 'warning' AS TipoMensaje
		ELSE
			SELECT 'Sucursal actualizada correctamente.' AS Mensaje, 'success' AS TipoMensaje
	END

	-- ELIMINAR (baja lógica; no si tiene personal o socios activos)
	IF @Accion = 5
	BEGIN
		IF EXISTS (SELECT 1 FROM EMPLEADO WHERE IdSucursal = @IdSucursal AND FechaBaja IS NULL)
		BEGIN
			SELECT 'No se puede eliminar: la sucursal tiene empleados activos.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF EXISTS (SELECT 1 FROM CLIENTE WHERE IdSucursal = @IdSucursal AND FechaBaja IS NULL)
		BEGIN
			SELECT 'No se puede eliminar: la sucursal tiene socios registrados.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		UPDATE SUCURSAL
		SET FechaBaja = GETDATE(), UsuarioBaja = @UsuarioSesion, IdEstatus = 2
		WHERE IdSucursal = @IdSucursal AND FechaBaja IS NULL

		IF @@ROWCOUNT = 0
			SELECT 'No se encontró la sucursal a eliminar.' AS Mensaje, 'warning' AS TipoMensaje
		ELSE
			SELECT 'Sucursal eliminada correctamente.' AS Mensaje, 'success' AS TipoMensaje
	END

	-- COMBO DE SUCURSALES ACTIVAS (lo usan empleados, socios, clases y control de acceso)
	IF @Accion = 6
	BEGIN
		SELECT IdSucursal, Nombre, AforoMaximo, HoraApertura, HoraCierre
		FROM SUCURSAL
		WHERE FechaBaja IS NULL AND IdEstatus = 1
		ORDER BY Nombre
	END
END
GO

CREATE OR ALTER PROCEDURE [dbo].[spControlAcceso]
	@Accion				INT				=	NULL,
	@IdSucursal			INT				=	NULL,
	@Folio				INT				=	NULL,
	@UsuarioSesion		INT				=	NULL
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @Hoy DATE = CAST(GETDATE() AS DATE)

	-- AFORO ACTUAL VS MÁXIMO (socios con entrada de hoy y sin salida)
	IF @Accion = 1
	BEGIN
		SELECT
			S.IdSucursal,
			S.Nombre,
			S.AforoMaximo,
			S.HoraApertura,
			S.HoraCierre,
			(SELECT COUNT(*) FROM CONTROL_ACCESO CA
			 WHERE CA.IdSucursal = S.IdSucursal AND CA.FechaSalida IS NULL AND CAST(CA.FechaEntrada AS DATE) = @Hoy) AS Ocupacion
		FROM SUCURSAL S
		WHERE S.IdSucursal = @IdSucursal AND S.FechaBaja IS NULL
	END

	-- REGISTRAR ENTRADA: semáforo Verde (Permitido) o Rojo (Denegado)
	IF @Accion = 2
	BEGIN
		EXEC spVencerSuscripciones

		DECLARE @IdCliente INT, @Socio NVARCHAR(160), @EstatusCliente INT
		SELECT @IdCliente = C.IdCliente,
			   @Socio = LTRIM(RTRIM(CONCAT(C.Nombre, ' ', C.PrimerApellido, ' ', ISNULL(C.SegundoApellido, '')))),
			   @EstatusCliente = C.IdEstatus
		FROM CLIENTE C
		WHERE C.Folio = @Folio AND C.FechaBaja IS NULL

		DECLARE @Membresia NVARCHAR(50), @Vence DATE
		SELECT TOP 1 @Membresia = TM.Nombre, @Vence = SU.FechaVencimiento
		FROM SUSCRIPCION SU
		INNER JOIN TIPO_MEMBRESIA TM ON TM.IdTipoMembresia = SU.IdTipoMembresia
		WHERE SU.IdCliente = @IdCliente AND SU.IdEstatus = 1 AND SU.FechaBaja IS NULL
		  AND @Hoy BETWEEN SU.FechaInicio AND SU.FechaVencimiento

		DECLARE @Resultado NVARCHAR(10) = 'Denegado', @Mensaje NVARCHAR(200)

		-- Datos de la sucursal y cuántos socios hay dentro
		DECLARE @AforoMaximo INT, @Ocupacion INT, @Abre TIME, @Cierra TIME
		DECLARE @HoraActual TIME = CAST(GETDATE() AS TIME)
		SELECT @AforoMaximo = AforoMaximo, @Abre = HoraApertura, @Cierra = HoraCierre
		FROM SUCURSAL
		WHERE IdSucursal = @IdSucursal AND IdEstatus = 1 AND FechaBaja IS NULL

		SELECT @Ocupacion = COUNT(*) FROM CONTROL_ACCESO
		WHERE IdSucursal = @IdSucursal AND FechaSalida IS NULL AND CAST(FechaEntrada AS DATE) = @Hoy

		IF @AforoMaximo IS NULL
			SET @Mensaje = 'La sucursal no está activa.'
		ELSE IF @HoraActual < @Abre OR @HoraActual > @Cierra
			SET @Mensaje = 'La sucursal está cerrada. Horario: ' + LEFT(CAST(@Abre AS NVARCHAR(8)), 5) + ' - ' + LEFT(CAST(@Cierra AS NVARCHAR(8)), 5) + '.'
		ELSE IF @IdCliente IS NULL
			SET @Mensaje = 'No existe un socio con el folio ' + ISNULL(CAST(@Folio AS NVARCHAR(10)), 'capturado') + '.'
		ELSE IF @EstatusCliente <> 1
			SET @Mensaje = 'El socio está inactivo.'
		ELSE IF @Membresia IS NULL
			SET @Mensaje = 'Membresía vencida o sin suscripción activa.'
		ELSE IF EXISTS (SELECT 1 FROM CONTROL_ACCESO WHERE IdCliente = @IdCliente AND FechaSalida IS NULL AND CAST(FechaEntrada AS DATE) = @Hoy)
			SET @Mensaje = 'El socio ya tiene una entrada registrada sin salida.'
		ELSE IF @Ocupacion >= @AforoMaximo
			SET @Mensaje = 'Aforo máximo alcanzado (' + CAST(@Ocupacion AS NVARCHAR(10)) + '/' + CAST(@AforoMaximo AS NVARCHAR(10)) + ').'
		ELSE
		BEGIN
			INSERT INTO CONTROL_ACCESO (IdSucursal, IdCliente, FechaEntrada) VALUES (@IdSucursal, @IdCliente, GETDATE())
			SET @Ocupacion = @Ocupacion + 1
			SET @Resultado = 'Permitido'
			SET @Mensaje = 'Acceso permitido. ¡Bienvenido!'
		END

		SELECT @Resultado AS Resultado, @Mensaje AS Mensaje, @Folio AS Folio, @Socio AS Socio,
			   @Membresia AS Membresia, @Vence AS FechaVencimiento, NULL AS Minutos,
			   ISNULL(@Ocupacion, 0) AS Ocupacion, ISNULL(@AforoMaximo, 0) AS AforoMaximo
	END

	-- REGISTRAR SALIDA: calcula automáticamente los minutos de permanencia
	IF @Accion = 3
	BEGIN
		DECLARE @IdControl INT, @Entrada DATETIME, @SocioSalida NVARCHAR(160), @Salida DATETIME = GETDATE()

		SELECT TOP 1 @IdControl = CA.IdControlAcceso, @Entrada = CA.FechaEntrada,
			   @SocioSalida = LTRIM(RTRIM(CONCAT(C.Nombre, ' ', C.PrimerApellido, ' ', ISNULL(C.SegundoApellido, ''))))
		FROM CONTROL_ACCESO CA
		INNER JOIN CLIENTE C ON C.IdCliente = CA.IdCliente
		WHERE C.Folio = @Folio AND CA.IdSucursal = @IdSucursal AND CA.FechaSalida IS NULL
		ORDER BY CA.FechaEntrada DESC

		IF @IdControl IS NULL
		BEGIN
			SELECT 'Denegado' AS Resultado, 'El folio ' + ISNULL(CAST(@Folio AS NVARCHAR(10)), 'capturado') + ' no tiene una entrada abierta en esta sucursal.' AS Mensaje,
				   @Folio AS Folio, NULL AS Socio, NULL AS Membresia, NULL AS FechaVencimiento, NULL AS Minutos,
				   (SELECT COUNT(*) FROM CONTROL_ACCESO WHERE IdSucursal = @IdSucursal AND FechaSalida IS NULL AND CAST(FechaEntrada AS DATE) = @Hoy) AS Ocupacion,
				   (SELECT AforoMaximo FROM SUCURSAL WHERE IdSucursal = @IdSucursal) AS AforoMaximo
			RETURN
		END

		UPDATE CONTROL_ACCESO SET FechaSalida = @Salida WHERE IdControlAcceso = @IdControl

		DECLARE @Minutos INT = DATEDIFF(MINUTE, @Entrada, @Salida)

		SELECT 'Salida' AS Resultado,
			   'Salida registrada. Permanencia: ' + CAST(@Minutos AS NVARCHAR(10)) + ' minutos.' AS Mensaje,
			   @Folio AS Folio, @SocioSalida AS Socio, NULL AS Membresia, NULL AS FechaVencimiento, @Minutos AS Minutos,
			   (SELECT COUNT(*) FROM CONTROL_ACCESO WHERE IdSucursal = @IdSucursal AND FechaSalida IS NULL AND CAST(FechaEntrada AS DATE) = @Hoy) AS Ocupacion,
			   (SELECT AforoMaximo FROM SUCURSAL WHERE IdSucursal = @IdSucursal) AS AforoMaximo
	END

	-- MOVIMIENTOS DE HOY EN LA SUCURSAL (los que siguen dentro primero)
	IF @Accion = 4
	BEGIN
		SELECT
			C.Folio,
			LTRIM(RTRIM(CONCAT(C.Nombre, ' ', C.PrimerApellido, ' ', ISNULL(C.SegundoApellido, '')))) AS Socio,
			CA.FechaEntrada,
			CA.FechaSalida,
			DATEDIFF(MINUTE, CA.FechaEntrada, ISNULL(CA.FechaSalida, GETDATE())) AS Minutos
		FROM CONTROL_ACCESO CA
		INNER JOIN CLIENTE C ON C.IdCliente = CA.IdCliente
		WHERE CA.IdSucursal = @IdSucursal
		  AND CAST(CA.FechaEntrada AS DATE) = @Hoy
		ORDER BY CASE WHEN CA.FechaSalida IS NULL THEN 0 ELSE 1 END, CA.FechaEntrada DESC
	END
END

GO

CREATE OR ALTER PROCEDURE [dbo].[spSocio]
	@Accion					INT				=	NULL,
	@IdCliente				INT				=	NULL,
	@IdSucursal				INT				=	NULL,
	@IdEstatus				INT				=	NULL,
	@Folio					INT				=	NULL,
	@Nombre					NVARCHAR(50)	=	NULL,	-- En la consulta: búsqueda por nombre o folio
	@PrimerApellido			NVARCHAR(50)	=	NULL,
	@SegundoApellido		NVARCHAR(50)	=	NULL,
	@FechaNacimiento		DATE			=	NULL,
	@Correo					NVARCHAR(50)	=	NULL,
	@Telefono				NVARCHAR(50)	=	NULL,
	-- Expediente médico (obligatorio al registrar)
	@TipoSangre				NVARCHAR(50)	=	NULL,
	@PadecimientosCronicos	NVARCHAR(50)	=	NULL,
	@LesionesPrevias		NVARCHAR(50)	=	NULL,
	@Alergias				NVARCHAR(50)	=	NULL,
	@CondicionRiesgo		NVARCHAR(50)	=	NULL,
	@IdTipoMembresia		INT				=	NULL,	-- Solo en la consulta (0 = sin membresía)
	@UsuarioSesion			INT				=	NULL
AS
BEGIN
	SET NOCOUNT ON;

	-- Textos sin acentos, en mayusculas y sin espacios de mas (dbo.fnRemoverTildes)
	SET @Nombre = dbo.fnRemoverTildes(LTRIM(RTRIM(@Nombre)))
	SET @PrimerApellido = dbo.fnRemoverTildes(LTRIM(RTRIM(@PrimerApellido)))
	SET @SegundoApellido = dbo.fnRemoverTildes(LTRIM(RTRIM(@SegundoApellido)))
	SET @PadecimientosCronicos = dbo.fnRemoverTildes(LTRIM(RTRIM(@PadecimientosCronicos)))
	SET @LesionesPrevias = dbo.fnRemoverTildes(LTRIM(RTRIM(@LesionesPrevias)))
	SET @Alergias = dbo.fnRemoverTildes(LTRIM(RTRIM(@Alergias)))
	SET @CondicionRiesgo = dbo.fnRemoverTildes(LTRIM(RTRIM(@CondicionRiesgo)))

	IF @Accion IN (1, 4)
	BEGIN
		IF EXISTS (SELECT 1 FROM CLIENTE WHERE Correo = @Correo AND FechaBaja IS NULL AND IdCliente <> ISNULL(@IdCliente, 0))
		BEGIN
			SELECT 'El correo ' + @Correo + ' ya pertenece a otro socio.' AS Mensaje, 'warning' AS TipoMensaje, 0 AS Folio
			RETURN
		END

		IF NOT EXISTS (SELECT 1 FROM SUCURSAL WHERE IdSucursal = @IdSucursal AND IdEstatus = 1 AND FechaBaja IS NULL)
		BEGIN
			SELECT 'La sucursal seleccionada no está activa.' AS Mensaje, 'warning' AS TipoMensaje, 0 AS Folio
			RETURN
		END
	END

	-- REGISTRAR SOCIO + EXPEDIENTE MÉDICO. El folio se genera aquí: Año actual + consecutivo de 4 dígitos (ej. 20260001)
	IF @Accion = 1
	BEGIN
		IF NULLIF(LTRIM(@TipoSangre), '') IS NULL
		BEGIN
			SELECT 'El expediente médico es obligatorio: indica al menos el tipo de sangre.' AS Mensaje, 'warning' AS TipoMensaje, 0 AS Folio
			RETURN
		END

		-- Siguiente folio del año
		DECLARE @Anio INT = YEAR(GETDATE())
		DECLARE @NuevoFolio INT

		SELECT @NuevoFolio = ISNULL(MAX(Folio), @Anio * 10000) + 1
		FROM CLIENTE
		WHERE Folio BETWEEN @Anio * 10000 AND @Anio * 10000 + 9999

		IF @NuevoFolio > @Anio * 10000 + 9999
		BEGIN
			SELECT 'Se alcanzó el máximo de folios del año.' AS Mensaje, 'danger' AS TipoMensaje, 0 AS Folio
			RETURN
		END

		INSERT INTO CLIENTE (IdSucursal, IdEstatus, Folio, Nombre, PrimerApellido, SegundoApellido, FechaNacimiento,
							 Correo, Telefono, FechaInscripcion, FechaRegistro, UsuarioRegistro)
		VALUES (@IdSucursal, @IdEstatus, @NuevoFolio, @Nombre, @PrimerApellido, @SegundoApellido, @FechaNacimiento,
				@Correo, @Telefono, GETDATE(), GETDATE(), @UsuarioSesion)

		DECLARE @IdNuevo INT = SCOPE_IDENTITY()

		INSERT INTO EXPEDIENTE_MEDICO (IdCliente, TipoSangre, PadecimientosCronicos, LesionesPrevias, Alergias, CondicionRiesgo)
		VALUES (@IdNuevo, @TipoSangre, @PadecimientosCronicos, @LesionesPrevias, @Alergias, @CondicionRiesgo)

		-- IdCliente: la página lo usa para mandar al socio nuevo a Suscripción (membresía obligatoria)
		SELECT 'Socio registrado correctamente con el folio ' + CAST(@NuevoFolio AS NVARCHAR(10)) + '.' AS Mensaje,
			   'success' AS TipoMensaje, @NuevoFolio AS Folio, @IdNuevo AS IdCliente
	END

	IF @Accion = 2
	BEGIN
		SELECT IdCliente, IdSucursal, IdEstatus, Folio, Nombre, PrimerApellido, SegundoApellido,
			   FechaNacimiento, Correo, Telefono, FechaInscripcion
		FROM CLIENTE
		WHERE IdCliente = @IdCliente AND FechaBaja IS NULL
	END

	-- CONSULTA GENERAL (con su membresía vigente y si tiene expediente)
	IF @Accion = 3
	BEGIN
		EXEC spVencerSuscripciones

		SELECT
			C.IdCliente,
			C.Folio,
			LTRIM(RTRIM(CONCAT(C.Nombre, ' ', C.PrimerApellido, ' ', ISNULL(C.SegundoApellido, '')))) AS Socio,
			C.Correo,
			C.Telefono,
			C.FechaInscripcion,
			S.Nombre			AS Sucursal,
			C.IdEstatus,
			ES.Descripcion		AS Estatus,
			M.Membresia,
			M.FechaVencimiento,
			CASE WHEN EXISTS (SELECT 1 FROM EXPEDIENTE_MEDICO EM WHERE EM.IdCliente = C.IdCliente) THEN 1 ELSE 0 END AS TieneExpediente
		FROM CLIENTE C
		INNER JOIN SUCURSAL S	ON S.IdSucursal = C.IdSucursal
		INNER JOIN ESTATUS ES	ON ES.IdEstatus = C.IdEstatus
		-- Suscripción activa más antigua = la actual (si renovó por adelantado, la otra empieza después)
		OUTER APPLY (SELECT TOP 1 SU.IdTipoMembresia, TM.Nombre AS Membresia, SU.FechaVencimiento
					 FROM SUSCRIPCION SU
					 INNER JOIN TIPO_MEMBRESIA TM ON TM.IdTipoMembresia = SU.IdTipoMembresia
					 WHERE SU.IdCliente = C.IdCliente AND SU.IdEstatus = 1 AND SU.FechaBaja IS NULL
					 ORDER BY SU.FechaInicio) M
		WHERE C.FechaBaja IS NULL
		  -- Filtro de membresía: -1 = con membresía activa (cualquiera), 0 = sin membresía activa
		  AND (@IdTipoMembresia IS NULL
			   OR (@IdTipoMembresia = -1 AND M.IdTipoMembresia IS NOT NULL)
			   OR (@IdTipoMembresia = 0 AND M.IdTipoMembresia IS NULL)
			   OR M.IdTipoMembresia = @IdTipoMembresia)
		  AND (@Nombre IS NULL
			   OR CAST(C.Folio AS NVARCHAR(10)) LIKE @Nombre + '%'
			   OR CONCAT(C.Nombre, ' ', C.PrimerApellido, ' ', ISNULL(C.SegundoApellido, '')) LIKE '%' + @Nombre + '%')
		  AND (@IdSucursal IS NULL OR C.IdSucursal = @IdSucursal)
		  AND (@IdEstatus IS NULL OR C.IdEstatus = @IdEstatus)
		ORDER BY C.Folio DESC
	END

	-- MODIFICAR (el folio nunca cambia)
	IF @Accion = 4
	BEGIN
		UPDATE CLIENTE
		SET IdSucursal			= @IdSucursal,
			IdEstatus			= @IdEstatus,
			Nombre				= @Nombre,
			PrimerApellido		= @PrimerApellido,
			SegundoApellido		= @SegundoApellido,
			FechaNacimiento		= @FechaNacimiento,
			Correo				= @Correo,
			Telefono			= @Telefono,
			FechaModificacion	= GETDATE(),
			UsuarioModificacion	= @UsuarioSesion
		WHERE IdCliente = @IdCliente AND FechaBaja IS NULL

		IF @@ROWCOUNT = 0
			SELECT 'No se encontró el socio a modificar.' AS Mensaje, 'warning' AS TipoMensaje, 0 AS Folio
		ELSE
			SELECT 'Socio actualizado correctamente.' AS Mensaje, 'success' AS TipoMensaje, 0 AS Folio
	END

	-- ELIMINAR (baja lógica; no si tiene suscripción activa)
	IF @Accion = 5
	BEGIN
		IF EXISTS (SELECT 1 FROM SUSCRIPCION WHERE IdCliente = @IdCliente AND IdEstatus = 1 AND FechaBaja IS NULL)
		BEGIN
			SELECT 'No se puede eliminar: el socio tiene una suscripción activa. Cancélala primero.' AS Mensaje, 'warning' AS TipoMensaje, 0 AS Folio
			RETURN
		END

		UPDATE CLIENTE
		SET FechaBaja = GETDATE(), UsuarioBaja = @UsuarioSesion, IdEstatus = 2
		WHERE IdCliente = @IdCliente AND FechaBaja IS NULL

		IF @@ROWCOUNT = 0
			SELECT 'No se encontró el socio a eliminar.' AS Mensaje, 'warning' AS TipoMensaje, 0 AS Folio
		ELSE
			SELECT 'Socio eliminado correctamente.' AS Mensaje, 'success' AS TipoMensaje, 0 AS Folio
	END

	-- COMBO DE SOCIOS ACTIVOS (suscripciones y reservas)
	IF @Accion = 6
	BEGIN
		SELECT
			C.IdCliente,
			CONCAT(C.Folio, ' - ', LTRIM(RTRIM(CONCAT(C.Nombre, ' ', C.PrimerApellido, ' ', ISNULL(C.SegundoApellido, ''))))) AS Socio
		FROM CLIENTE C
		WHERE C.IdEstatus = 1 AND C.FechaBaja IS NULL
		ORDER BY C.Nombre, C.PrimerApellido
	END
END

GO

CREATE OR ALTER PROCEDURE [dbo].[spSuscripcion]
	@Accion				INT				=	NULL,
	@IdSuscripcion		INT				=	NULL,
	@IdCliente			INT				=	NULL,
	@IdTipoMembresia	INT				=	NULL,
	@IdEstatus			INT				=	NULL,
	@FechaInicio		DATE			=	NULL,
	@Busqueda			NVARCHAR(50)	=	NULL,
	@UsuarioSesion		INT				=	NULL
AS
BEGIN
	SET NOCOUNT ON;

	-- Textos sin acentos, en mayusculas y sin espacios de mas (dbo.fnRemoverTildes)
	SET @Busqueda = dbo.fnRemoverTildes(LTRIM(RTRIM(@Busqueda)))

	EXEC spVencerSuscripciones

	-- VENDER / RENOVAR: vigencia automática de 1 mes y máximo 1 suscripción activa por socio
	IF @Accion = 1
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM CLIENTE WHERE IdCliente = @IdCliente AND IdEstatus = 1 AND FechaBaja IS NULL)
		BEGIN
			SELECT 'El socio seleccionado no está activo.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF NOT EXISTS (SELECT 1 FROM TIPO_MEMBRESIA WHERE IdTipoMembresia = @IdTipoMembresia AND IdEstatus = 1 AND FechaBaja IS NULL)
		BEGIN
			SELECT 'La membresía seleccionada no está activa.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF @FechaInicio IS NULL OR @FechaInicio < CAST(GETDATE() AS DATE)
		BEGIN
			SELECT 'La fecha de inicio no puede ser anterior a hoy.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF @FechaInicio > DATEADD(MONTH, 1, CAST(GETDATE() AS DATE))
		BEGIN
			SELECT 'La fecha de inicio no puede ser mayor a un mes a partir de hoy.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF @FechaInicio < (SELECT CAST(FechaInscripcion AS DATE) FROM CLIENTE WHERE IdCliente = @IdCliente)
		BEGIN
			SELECT 'La fecha de inicio no puede ser anterior a la inscripción del socio.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		-- Máximo 1 suscripción activa por socio
		DECLARE @Vence DATE = (SELECT TOP 1 FechaVencimiento FROM SUSCRIPCION
							   WHERE IdCliente = @IdCliente AND IdEstatus = 1 AND FechaBaja IS NULL)
		IF @Vence IS NOT NULL
		BEGIN
			SELECT 'El socio ya tiene una suscripción activa (vence el ' + CONVERT(NVARCHAR(10), @Vence, 103)
				   + '). Para agregar otro mes usa el botón Renovar.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		INSERT INTO SUSCRIPCION (IdEstatus, IdCliente, IdTipoMembresia, FechaInicio, FechaVencimiento, FechaRegistro, UsuarioRegistro)
		VALUES (1, @IdCliente, @IdTipoMembresia, @FechaInicio, DATEADD(DAY, -1, DATEADD(MONTH, 1, @FechaInicio)), GETDATE(), @UsuarioSesion)

		SELECT 'Suscripción registrada. Vigente del ' + CONVERT(NVARCHAR(10), @FechaInicio, 103) + ' al '
			   + CONVERT(NVARCHAR(10), DATEADD(DAY, -1, DATEADD(MONTH, 1, @FechaInicio)), 103) + '.' AS Mensaje, 'success' AS TipoMensaje
	END

	-- CONSULTA GENERAL
	IF @Accion = 3
	BEGIN
		SELECT
			SU.IdSuscripcion,
			SU.IdCliente,
			SU.IdTipoMembresia,
			C.Folio,
			LTRIM(RTRIM(CONCAT(C.Nombre, ' ', C.PrimerApellido, ' ', ISNULL(C.SegundoApellido, '')))) AS Socio,
			TM.Nombre			AS Membresia,
			TM.Precio,
			SU.FechaInicio,
			SU.FechaVencimiento,
			DATEDIFF(DAY, CAST(GETDATE() AS DATE), SU.FechaVencimiento) AS DiasRestantes,
			SU.IdEstatus,
			ES.Descripcion		AS Estatus,
			-- Renovación ya pagada que todavía no empieza
			CASE WHEN SU.IdEstatus = 1 AND SU.FechaInicio > CAST(GETDATE() AS DATE) THEN 1 ELSE 0 END AS Programada,
			-- Se renueva desde la última suscripción del socio (activa, vencida o cancelada),
			-- siempre que no tenga ya una renovación programada
			CASE WHEN NOT EXISTS (SELECT 1 FROM SUSCRIPCION X WHERE X.IdCliente = SU.IdCliente AND X.FechaBaja IS NULL AND X.IdSuscripcion > SU.IdSuscripcion)
					  AND NOT (SU.IdEstatus = 1 AND SU.FechaInicio > CAST(GETDATE() AS DATE))
				 THEN 1 ELSE 0 END AS PuedeRenovar
		FROM SUSCRIPCION SU
		INNER JOIN CLIENTE C			ON C.IdCliente = SU.IdCliente
		INNER JOIN TIPO_MEMBRESIA TM	ON TM.IdTipoMembresia = SU.IdTipoMembresia
		INNER JOIN ESTATUS ES			ON ES.IdEstatus = SU.IdEstatus
		WHERE SU.FechaBaja IS NULL
		  AND C.FechaBaja IS NULL
		  AND (@Busqueda IS NULL
			   OR CAST(C.Folio AS NVARCHAR(10)) LIKE @Busqueda + '%'
			   OR CONCAT(C.Nombre, ' ', C.PrimerApellido, ' ', ISNULL(C.SegundoApellido, '')) LIKE '%' + @Busqueda + '%')
		  AND (@IdTipoMembresia IS NULL OR SU.IdTipoMembresia = @IdTipoMembresia)
		  AND (@IdEstatus IS NULL OR SU.IdEstatus = @IdEstatus)
		ORDER BY SU.FechaRegistro DESC
	END

	-- CANCELAR UNA SUSCRIPCIÓN ACTIVA
	IF @Accion = 4
	BEGIN
		UPDATE SUSCRIPCION
		SET IdEstatus = 4, FechaModificacion = GETDATE(), UsuarioModificacion = @UsuarioSesion
		WHERE IdSuscripcion = @IdSuscripcion AND IdEstatus = 1 AND FechaBaja IS NULL

		IF @@ROWCOUNT = 0
			SELECT 'Solo se pueden cancelar suscripciones activas.' AS Mensaje, 'warning' AS TipoMensaje
		ELSE
			SELECT 'Suscripción cancelada correctamente.' AS Mensaje, 'success' AS TipoMensaje
	END

	-- RENOVAR (se puede en cualquier momento, con el mismo plan de la última suscripción):
	--   * Activa    -> otro mes que empieza al día siguiente de su vencimiento
	--   * Cancelada y con días -> se reanuda la misma suscripción (conserva los días que le quedaban)
	--   * Vencida, o cancelada sin días -> otro mes a partir de hoy
	IF @Accion = 6
	BEGIN
		DECLARE @Hoy DATE = CAST(GETDATE() AS DATE)
		DECLARE @Cliente INT, @Tipo INT, @Estatus INT, @Inicio DATE, @Fin DATE

		SELECT @Cliente = IdCliente, @Tipo = IdTipoMembresia, @Estatus = IdEstatus, @Inicio = FechaInicio, @Fin = FechaVencimiento
		FROM SUSCRIPCION
		WHERE IdSuscripcion = @IdSuscripcion AND FechaBaja IS NULL

		IF @Cliente IS NULL
		BEGIN
			SELECT 'No se encontró la suscripción.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF NOT EXISTS (SELECT 1 FROM CLIENTE WHERE IdCliente = @Cliente AND IdEstatus = 1 AND FechaBaja IS NULL)
		BEGIN
			SELECT 'El socio no está activo.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF NOT EXISTS (SELECT 1 FROM TIPO_MEMBRESIA WHERE IdTipoMembresia = @Tipo AND IdEstatus = 1 AND FechaBaja IS NULL)
		BEGIN
			SELECT 'La membresía de esta suscripción ya no está activa; véndele una nueva.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF EXISTS (SELECT 1 FROM SUSCRIPCION WHERE IdCliente = @Cliente AND FechaBaja IS NULL AND IdSuscripcion > @IdSuscripcion)
		BEGIN
			SELECT 'Solo se puede renovar la última suscripción del socio.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF @Estatus = 1 AND @Inicio > @Hoy
		BEGIN
			SELECT 'El socio ya tiene una renovación programada (inicia el ' + CONVERT(NVARCHAR(10), @Inicio, 103) + ').' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		-- Reanudar la cancelada que todavía tiene días
		IF @Estatus = 4 AND @Fin >= @Hoy
		BEGIN
			UPDATE SUSCRIPCION
			SET IdEstatus = 1, FechaModificacion = GETDATE(), UsuarioModificacion = @UsuarioSesion
			WHERE IdSuscripcion = @IdSuscripcion

			SELECT 'Membresía reanudada. Vigente hasta el ' + CONVERT(NVARCHAR(10), @Fin, 103) + '.' AS Mensaje, 'success' AS TipoMensaje
			RETURN
		END

		-- Nuevo mes: encadenado si sigue activa, o desde hoy
		DECLARE @NuevoInicio DATE = @Hoy
		IF @Estatus = 1
			SET @NuevoInicio = DATEADD(DAY, 1, @Fin)
		DECLARE @NuevoFin DATE = DATEADD(DAY, -1, DATEADD(MONTH, 1, @NuevoInicio))

		INSERT INTO SUSCRIPCION (IdEstatus, IdCliente, IdTipoMembresia, FechaInicio, FechaVencimiento, FechaRegistro, UsuarioRegistro)
		VALUES (1, @Cliente, @Tipo, @NuevoInicio, @NuevoFin, GETDATE(), @UsuarioSesion)

		SELECT 'Suscripción renovada. Vigente del ' + CONVERT(NVARCHAR(10), @NuevoInicio, 103) + ' al '
			   + CONVERT(NVARCHAR(10), @NuevoFin, 103) + '.' AS Mensaje, 'success' AS TipoMensaje
	END
END

GO

CREATE OR ALTER PROCEDURE [dbo].[spReserva]
	@Accion				INT				=	NULL,
	@IdReserva			INT				=	NULL,
	@IdCliente			INT				=	NULL,
	@IdClase			INT				=	NULL,
	@IdEmpleado			INT				=	NULL,	-- Recepcionista que captura
	@IdEstatus			INT				=	NULL,
	@Busqueda			NVARCHAR(50)	=	NULL,
	@Fecha				DATE			=	NULL,
	@UsuarioSesion		INT				=	NULL
AS
BEGIN
	SET NOCOUNT ON;

	-- Textos sin acentos, en mayusculas y sin espacios de mas (dbo.fnRemoverTildes)
	SET @Busqueda = dbo.fnRemoverTildes(LTRIM(RTRIM(@Busqueda)))

	-- CAPTURAR RESERVA: cupo del salón + límite semanal del plan (Bronce 0, Plata 3, Oro 5)
	IF @Accion = 1
	BEGIN
		EXEC spVencerSuscripciones

		-- Datos de la clase
		DECLARE @FechaClase DATE, @HoraClase TIME, @Cupo INT
		SELECT @FechaClase = Fecha, @HoraClase = HoraInicio, @Cupo = CupoMaximo
		FROM CLASE
		WHERE IdClase = @IdClase AND IdEstatus = 1 AND FechaBaja IS NULL

		IF @FechaClase IS NULL
		BEGIN
			SELECT 'La clase seleccionada no existe o fue cancelada.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF CAST(@FechaClase AS DATETIME) + CAST(@HoraClase AS DATETIME) <= GETDATE()
		BEGIN
			SELECT 'La clase ya comenzó; no se pueden capturar reservas.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF NOT EXISTS (SELECT 1 FROM CLIENTE WHERE IdCliente = @IdCliente AND IdEstatus = 1 AND FechaBaja IS NULL)
		BEGIN
			SELECT 'El socio seleccionado no está activo.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		-- Plan vigente el día de la clase
		DECLARE @Plan NVARCHAR(50), @MaxClases INT
		SELECT TOP 1 @Plan = TM.Nombre, @MaxClases = TM.MaxClasesSemana
		FROM SUSCRIPCION SU
		INNER JOIN TIPO_MEMBRESIA TM ON TM.IdTipoMembresia = SU.IdTipoMembresia
		WHERE SU.IdCliente = @IdCliente AND SU.IdEstatus = 1 AND SU.FechaBaja IS NULL
		  AND @FechaClase BETWEEN SU.FechaInicio AND SU.FechaVencimiento

		IF @Plan IS NULL
		BEGIN
			SELECT 'El socio no tiene una membresía vigente para la fecha de la clase.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF @MaxClases = 0
		BEGIN
			SELECT 'La membresía ' + @Plan + ' no incluye clases grupales.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF EXISTS (SELECT 1 FROM RESERVA WHERE IdCliente = @IdCliente AND IdClase = @IdClase AND IdEstatus IN (5, 6, 7) AND FechaBaja IS NULL)
		BEGIN
			SELECT 'El socio ya tiene una reserva en esta clase.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		-- Límite semanal (semana de lunes a domingo de la clase)
		DECLARE @Lunes DATE = DATEADD(DAY, -(DATEDIFF(DAY, '19000101', @FechaClase) % 7), @FechaClase)
		DECLARE @Usadas INT
		SELECT @Usadas = COUNT(*)
		FROM RESERVA R
		INNER JOIN CLASE CL ON CL.IdClase = R.IdClase
		WHERE R.IdCliente = @IdCliente AND R.IdEstatus IN (5, 6, 7) AND R.FechaBaja IS NULL
		  AND CL.Fecha BETWEEN @Lunes AND DATEADD(DAY, 6, @Lunes)

		IF @Usadas >= @MaxClases
		BEGIN
			SELECT 'Límite semanal alcanzado: la membresía ' + @Plan + ' permite ' + CAST(@MaxClases AS NVARCHAR(5))
				   + ' clases por semana y ya tiene ' + CAST(@Usadas AS NVARCHAR(5)) + '.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		-- Cupo del salón
		IF (SELECT COUNT(*) FROM RESERVA WHERE IdClase = @IdClase AND IdEstatus IN (5, 6, 7) AND FechaBaja IS NULL) >= @Cupo
		BEGIN
			SELECT 'La clase ya no tiene lugares disponibles (cupo de ' + CAST(@Cupo AS NVARCHAR(5)) + ').' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		INSERT INTO RESERVA (FechaSolicitud, IdCliente, IdEmpleado, IdClase, IdEstatus, FechaRegistro, UsuarioRegistro)
		VALUES (GETDATE(), @IdCliente, @IdEmpleado, @IdClase, 5, GETDATE(), @UsuarioSesion)

		SELECT 'Reserva confirmada (' + CAST(@Usadas + 1 AS NVARCHAR(5)) + ' de ' + CAST(@MaxClases AS NVARCHAR(5))
			   + ' clases de la semana, plan ' + @Plan + ').' AS Mensaje, 'success' AS TipoMensaje
	END

	-- CANCELAR RESERVA (solo confirmadas y antes de que empiece la clase)
	IF @Accion = 2
	BEGIN
		UPDATE R
		SET R.IdEstatus = 4, R.FechaModificacion = GETDATE(), R.UsuarioModificacion = @UsuarioSesion
		FROM RESERVA R
		INNER JOIN CLASE CL ON CL.IdClase = R.IdClase
		WHERE R.IdReserva = @IdReserva AND R.IdEstatus = 5 AND R.FechaBaja IS NULL
		  AND CAST(CL.Fecha AS DATETIME) + CAST(CL.HoraInicio AS DATETIME) > GETDATE()

		IF @@ROWCOUNT = 0
			SELECT 'Solo se pueden cancelar reservas confirmadas de clases que no han comenzado.' AS Mensaje, 'warning' AS TipoMensaje
		ELSE
			SELECT 'Reserva cancelada correctamente.' AS Mensaje, 'success' AS TipoMensaje
	END

	-- CONSULTA GENERAL
	IF @Accion = 3
	BEGIN
		SELECT
			R.IdReserva,
			C.Folio,
			LTRIM(RTRIM(CONCAT(C.Nombre, ' ', C.PrimerApellido, ' ', ISNULL(C.SegundoApellido, '')))) AS Socio,
			CL.Nombre		AS Clase,
			CL.Fecha,
			CL.HoraInicio,
			CL.Salon,
			S.Nombre		AS Sucursal,
			R.FechaSolicitud,
			R.IdEstatus,
			ES.Descripcion	AS Estatus,
			CASE WHEN R.IdEstatus = 5 AND CAST(CL.Fecha AS DATETIME) + CAST(CL.HoraInicio AS DATETIME) > GETDATE() THEN 1 ELSE 0 END AS PuedeCancelar
		FROM RESERVA R
		INNER JOIN CLIENTE C	ON C.IdCliente = R.IdCliente
		INNER JOIN CLASE CL		ON CL.IdClase = R.IdClase
		INNER JOIN SUCURSAL S	ON S.IdSucursal = CL.IdSucursal
		INNER JOIN ESTATUS ES	ON ES.IdEstatus = R.IdEstatus
		WHERE R.FechaBaja IS NULL
		  AND (@Busqueda IS NULL
			   OR CAST(C.Folio AS NVARCHAR(10)) LIKE @Busqueda + '%'
			   OR CONCAT(C.Nombre, ' ', C.PrimerApellido, ' ', ISNULL(C.SegundoApellido, '')) LIKE '%' + @Busqueda + '%')
		  AND (@IdClase IS NULL OR R.IdClase = @IdClase)
		  AND (@Fecha IS NULL OR CL.Fecha = @Fecha)
		  AND (@IdEstatus IS NULL OR R.IdEstatus = @IdEstatus)
		ORDER BY CL.Fecha DESC, CL.HoraInicio DESC
	END

	-- COMBO DE CLASES CON LUGAR (futuras, activas)
	IF @Accion = 4
	BEGIN
		SELECT
			CL.IdClase,
			CL.Nombre + ' · ' + CONVERT(NVARCHAR(5), CL.Fecha, 103) + ' ' + LEFT(CAST(CL.HoraInicio AS NVARCHAR(8)), 5)
				+ ' · ' + S.Nombre + ' / ' + CL.Salon
				+ ' (' + CAST(CL.CupoMaximo - X.Reservadas AS NVARCHAR(5)) + ' lugares)' AS Clase
		FROM CLASE CL
		INNER JOIN SUCURSAL S ON S.IdSucursal = CL.IdSucursal
		CROSS APPLY (SELECT COUNT(*) AS Reservadas FROM RESERVA R WHERE R.IdClase = CL.IdClase AND R.IdEstatus IN (5, 6, 7) AND R.FechaBaja IS NULL) X
		WHERE CL.FechaBaja IS NULL
		  AND CL.IdEstatus = 1
		  AND CAST(CL.Fecha AS DATETIME) + CAST(CL.HoraInicio AS DATETIME) > GETDATE()
		  AND X.Reservadas < CL.CupoMaximo
		ORDER BY CL.Fecha, CL.HoraInicio
	END

	-- PLAN DEL SOCIO Y CLASES USADAS EN LA SEMANA (panel informativo al capturar)
	IF @Accion = 5
	BEGIN
		EXEC spVencerSuscripciones

		-- La semana que se revisa es la de la clase elegida (puede ser de semanas siguientes); sin clase, la actual
		DECLARE @Ref DATE = ISNULL(@Fecha, CAST(GETDATE() AS DATE))
		IF @IdClase IS NOT NULL
			SELECT @Ref = Fecha FROM CLASE WHERE IdClase = @IdClase
		DECLARE @Lun DATE = DATEADD(DAY, -(DATEDIFF(DAY, '19000101', @Ref) % 7), @Ref)

		SELECT
			M.Membresia,
			M.MaxClasesSemana,
			M.FechaVencimiento,
			@Lun AS Lunes,
			(SELECT COUNT(*) FROM RESERVA R INNER JOIN CLASE CL ON CL.IdClase = R.IdClase
			 WHERE R.IdCliente = @IdCliente AND R.IdEstatus IN (5, 6, 7) AND R.FechaBaja IS NULL
			   AND CL.Fecha BETWEEN @Lun AND DATEADD(DAY, 6, @Lun)) AS Usadas
		FROM CLIENTE C
		-- Plan vigente en esa fecha (igual que la validación al reservar)
		OUTER APPLY (SELECT TOP 1 TM.Nombre AS Membresia, TM.MaxClasesSemana, SU.FechaVencimiento
					 FROM SUSCRIPCION SU
					 INNER JOIN TIPO_MEMBRESIA TM ON TM.IdTipoMembresia = SU.IdTipoMembresia
					 WHERE SU.IdCliente = C.IdCliente AND SU.IdEstatus = 1 AND SU.FechaBaja IS NULL
					   AND @Ref BETWEEN SU.FechaInicio AND SU.FechaVencimiento) M
		WHERE C.IdCliente = @IdCliente
	END
END
GO

CREATE OR ALTER PROCEDURE [dbo].[spPaseLista]
	@Accion				INT				=	NULL,
	@IdClase			INT				=	NULL,
	@IdEmpleado			INT				=	NULL,	-- Entrenador (NULL = todas las clases, para el Administrador)
	@Fecha				DATE			=	NULL,
	@IdReserva			INT				=	NULL,	-- Pase de lista: un socio a la vez
	@IdEstatus			INT				=	NULL,	-- 5 Confirmado, 6 Asistió, 7 Falta
	@SoloValidar		BIT				=	0,		-- 1 = solo revisa las reglas, no guarda
	@UsuarioSesion		INT				=	NULL
AS
BEGIN
	SET NOCOUNT ON;

	-- CLASES DEL DÍA (agenda del entrenador)
	IF @Accion = 1
	BEGIN
		SELECT
			CL.IdClase,
			CL.Nombre,
			CL.HoraInicio,
			CL.HoraFin,
			CL.Salon,
			CL.CupoMaximo,
			S.Nombre AS Sucursal,
			LTRIM(RTRIM(CONCAT(E.Nombre, ' ', E.PrimerApellido))) AS Entrenador,
			SUM(CASE WHEN R.IdEstatus IN (5, 6, 7) THEN 1 ELSE 0 END)	AS Reservadas,
			SUM(CASE WHEN R.IdEstatus = 6 THEN 1 ELSE 0 END)			AS Asistieron,
			SUM(CASE WHEN R.IdEstatus = 7 THEN 1 ELSE 0 END)			AS Faltas,
			SUM(CASE WHEN R.IdEstatus = 5 THEN 1 ELSE 0 END)			AS Pendientes,
			CASE WHEN CAST(CL.Fecha AS DATETIME) + CAST(CL.HoraInicio AS DATETIME) <= GETDATE() THEN 1 ELSE 0 END AS PuedePasarLista
		FROM CLASE CL
		INNER JOIN SUCURSAL S	ON S.IdSucursal = CL.IdSucursal
		INNER JOIN EMPLEADO E	ON E.IdEmpleado = CL.IdEmpleado
		LEFT JOIN RESERVA R		ON R.IdClase = CL.IdClase AND R.FechaBaja IS NULL
		WHERE CL.FechaBaja IS NULL
		  AND CL.Fecha = ISNULL(@Fecha, CAST(GETDATE() AS DATE))
		  AND (@IdEmpleado IS NULL OR CL.IdEmpleado = @IdEmpleado)
		GROUP BY CL.IdClase, CL.Nombre, CL.Fecha, CL.HoraInicio, CL.HoraFin, CL.Salon, CL.CupoMaximo, S.Nombre, E.Nombre, E.PrimerApellido
		ORDER BY CL.HoraInicio
	END

	-- PARTICIPANTES DE UNA CLASE
	IF @Accion = 2
	BEGIN
		SELECT
			R.IdReserva,
			C.Folio,
			LTRIM(RTRIM(CONCAT(C.Nombre, ' ', C.PrimerApellido, ' ', ISNULL(C.SegundoApellido, '')))) AS Socio,
			R.IdEstatus,
			ES.Descripcion AS Estatus,
			-- 1 si el socio registró entrada en la sucursal de la clase ese mismo día
			CASE WHEN EXISTS (SELECT 1 FROM CONTROL_ACCESO CA
							  WHERE CA.IdCliente = R.IdCliente AND CA.IdSucursal = CL.IdSucursal
								AND CAST(CA.FechaEntrada AS DATE) = CL.Fecha) THEN 1 ELSE 0 END AS TuvoEntrada
		FROM RESERVA R
		INNER JOIN CLIENTE C	ON C.IdCliente = R.IdCliente
		INNER JOIN CLASE CL		ON CL.IdClase = R.IdClase
		INNER JOIN ESTATUS ES	ON ES.IdEstatus = R.IdEstatus
		WHERE R.IdClase = @IdClase
		  AND R.FechaBaja IS NULL
		  AND R.IdEstatus IN (5, 6, 7)
		  AND (@IdEmpleado IS NULL OR CL.IdEmpleado = @IdEmpleado)
		ORDER BY C.Nombre, C.PrimerApellido
	END

	-- GUARDAR ASISTENCIA DE UN SOCIO (Confirmado 5 / Asistió 6 / Falta 7)
	-- La página la llama una vez por cada socio. Con @SoloValidar = 1 solo revisa las reglas (no guarda):
	-- así primero se validan todos y, si nadie tiene error, se guardan todos.
	IF @Accion = 3
	BEGIN
		DECLARE @Inicio DATETIME, @Entrenador INT, @SucursalClase INT, @FechaClase DATE
		SELECT @Inicio = CAST(Fecha AS DATETIME) + CAST(HoraInicio AS DATETIME), @Entrenador = IdEmpleado,
			   @SucursalClase = IdSucursal, @FechaClase = Fecha
		FROM CLASE WHERE IdClase = @IdClase AND FechaBaja IS NULL

		IF @Inicio IS NULL
		BEGIN
			SELECT 'No se encontró la clase.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF @IdEmpleado IS NOT NULL AND @Entrenador <> @IdEmpleado
		BEGIN
			SELECT 'Solo puedes pasar lista en tus propias clases.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF @Inicio > GETDATE()
		BEGIN
			SELECT 'La clase todavía no comienza; el pase de lista se habilita a la hora de inicio.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF @IdEstatus IS NULL OR @IdEstatus NOT IN (5, 6, 7)
		BEGIN
			SELECT 'Hay participantes con un estatus de asistencia no válido.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		-- La reserva debe ser de esta clase y seguir vigente
		DECLARE @IdCliente INT, @Folio INT
		SELECT @IdCliente = R.IdCliente, @Folio = C.Folio
		FROM RESERVA R
		INNER JOIN CLIENTE C ON C.IdCliente = R.IdCliente
		WHERE R.IdReserva = @IdReserva AND R.IdClase = @IdClase AND R.FechaBaja IS NULL AND R.IdEstatus IN (5, 6, 7)

		IF @IdCliente IS NULL
		BEGIN
			SELECT 'Hay participantes que no pertenecen a esta clase.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		-- Solo puede marcarse "Asistió" si el socio registró su entrada en la sucursal de la clase ese día
		IF @IdEstatus = 6 AND NOT EXISTS (SELECT 1 FROM CONTROL_ACCESO
										  WHERE IdCliente = @IdCliente AND IdSucursal = @SucursalClase
											AND CAST(FechaEntrada AS DATE) = @FechaClase)
		BEGIN
			SELECT 'No se puede marcar asistencia sin entrada registrada en la sucursal ese día (folio ' + CAST(@Folio AS NVARCHAR(10)) + ').' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF @SoloValidar = 1
		BEGIN
			SELECT 'Correcto.' AS Mensaje, 'success' AS TipoMensaje
			RETURN
		END

		UPDATE RESERVA
		SET IdEstatus = @IdEstatus, FechaModificacion = GETDATE(), UsuarioModificacion = @UsuarioSesion
		WHERE IdReserva = @IdReserva

		SELECT 'Asistencia guardada.' AS Mensaje, 'success' AS TipoMensaje
	END
END
GO

-- =============================================================
-- PANTALLA DE INICIO: resumen del día (Administrador = todas las sucursales,
-- Recepcionista = solo la suya). La agenda del entrenador usa spClase (Accion 3).
-- =============================================================
CREATE OR ALTER PROCEDURE [dbo].[spInicio]
	@Accion				INT				=	NULL,
	@IdSucursal			INT				=	NULL	-- NULL = todas las sucursales
AS
BEGIN
	SET NOCOUNT ON;

	EXEC spVencerSuscripciones

	DECLARE @Hoy DATE = CAST(GETDATE() AS DATE)

	-- INDICADORES (un solo renglón)
	IF @Accion = 1
	BEGIN
		SELECT
			-- Socios activos y cuántos tienen membresía vigente hoy
			(SELECT COUNT(*) FROM CLIENTE C
			 WHERE C.FechaBaja IS NULL AND C.IdEstatus = 1
			   AND (@IdSucursal IS NULL OR C.IdSucursal = @IdSucursal)) AS SociosActivos,
			(SELECT COUNT(*) FROM CLIENTE C
			 WHERE C.FechaBaja IS NULL AND C.IdEstatus = 1
			   AND (@IdSucursal IS NULL OR C.IdSucursal = @IdSucursal)
			   AND EXISTS (SELECT 1 FROM SUSCRIPCION SU WHERE SU.IdCliente = C.IdCliente AND SU.IdEstatus = 1
							 AND SU.FechaBaja IS NULL AND @Hoy BETWEEN SU.FechaInicio AND SU.FechaVencimiento)) AS ConMembresia,
			-- Membresías que vencen en los próximos 7 días (sin renovación programada)
			(SELECT COUNT(*) FROM SUSCRIPCION SU
			 INNER JOIN CLIENTE C ON C.IdCliente = SU.IdCliente
			 WHERE SU.IdEstatus = 1 AND SU.FechaBaja IS NULL AND C.FechaBaja IS NULL
			   AND SU.FechaVencimiento BETWEEN @Hoy AND DATEADD(DAY, 7, @Hoy)
			   AND NOT EXISTS (SELECT 1 FROM SUSCRIPCION X WHERE X.IdCliente = SU.IdCliente AND X.IdEstatus = 1
								 AND X.FechaBaja IS NULL AND X.FechaInicio > SU.FechaVencimiento)
			   AND (@IdSucursal IS NULL OR C.IdSucursal = @IdSucursal)) AS PorVencer,
			-- Acceso de hoy
			(SELECT COUNT(*) FROM CONTROL_ACCESO CA
			 WHERE CAST(CA.FechaEntrada AS DATE) = @Hoy
			   AND (@IdSucursal IS NULL OR CA.IdSucursal = @IdSucursal)) AS EntradasHoy,
			(SELECT COUNT(*) FROM CONTROL_ACCESO CA
			 WHERE CAST(CA.FechaEntrada AS DATE) = @Hoy AND CA.FechaSalida IS NULL
			   AND (@IdSucursal IS NULL OR CA.IdSucursal = @IdSucursal)) AS DentroAhora,
			(SELECT ISNULL(SUM(S.AforoMaximo), 0) FROM SUCURSAL S
			 WHERE S.FechaBaja IS NULL AND S.IdEstatus = 1
			   AND (@IdSucursal IS NULL OR S.IdSucursal = @IdSucursal)) AS AforoMaximo,
			-- Clases y reservas de hoy
			(SELECT COUNT(*) FROM CLASE CL
			 WHERE CL.FechaBaja IS NULL AND CL.Fecha = @Hoy
			   AND (@IdSucursal IS NULL OR CL.IdSucursal = @IdSucursal)) AS ClasesHoy,
			(SELECT COUNT(*) FROM RESERVA R
			 INNER JOIN CLASE CL ON CL.IdClase = R.IdClase
			 WHERE R.FechaBaja IS NULL AND R.IdEstatus IN (5, 6, 7) AND CL.FechaBaja IS NULL AND CL.Fecha = @Hoy
			   AND (@IdSucursal IS NULL OR CL.IdSucursal = @IdSucursal)) AS ReservasHoy,
			-- Ventas de membresías del mes (suscripciones registradas este mes, no canceladas)
			(SELECT ISNULL(SUM(TM.Precio), 0) FROM SUSCRIPCION SU
			 INNER JOIN TIPO_MEMBRESIA TM ON TM.IdTipoMembresia = SU.IdTipoMembresia
			 INNER JOIN CLIENTE C ON C.IdCliente = SU.IdCliente
			 WHERE SU.FechaBaja IS NULL AND SU.IdEstatus <> 4
			   AND YEAR(SU.FechaRegistro) = YEAR(@Hoy) AND MONTH(SU.FechaRegistro) = MONTH(@Hoy)
			   AND (@IdSucursal IS NULL OR C.IdSucursal = @IdSucursal)) AS VentasMes
	END

	-- MEMBRESÍAS POR VENCER (próximos 7 días, sin renovación programada)
	IF @Accion = 2
	BEGIN
		SELECT
			C.Folio,
			LTRIM(RTRIM(CONCAT(C.Nombre, ' ', C.PrimerApellido, ' ', ISNULL(C.SegundoApellido, '')))) AS Socio,
			C.Telefono,
			TM.Nombre AS Membresia,
			SU.FechaVencimiento,
			DATEDIFF(DAY, @Hoy, SU.FechaVencimiento) AS Dias
		FROM SUSCRIPCION SU
		INNER JOIN CLIENTE C			ON C.IdCliente = SU.IdCliente
		INNER JOIN TIPO_MEMBRESIA TM	ON TM.IdTipoMembresia = SU.IdTipoMembresia
		WHERE SU.IdEstatus = 1 AND SU.FechaBaja IS NULL AND C.FechaBaja IS NULL
		  AND SU.FechaVencimiento BETWEEN @Hoy AND DATEADD(DAY, 7, @Hoy)
		  AND NOT EXISTS (SELECT 1 FROM SUSCRIPCION X WHERE X.IdCliente = SU.IdCliente AND X.IdEstatus = 1
							AND X.FechaBaja IS NULL AND X.FechaInicio > SU.FechaVencimiento)
		  AND (@IdSucursal IS NULL OR C.IdSucursal = @IdSucursal)
		ORDER BY SU.FechaVencimiento, C.Nombre
	END
END
GO

CREATE OR ALTER PROCEDURE [dbo].[spEmpleado]
	@Accion				INT				=	NULL,
	@IdEmpleado			INT				=	NULL,
	@IdSucursal			INT				=	NULL,
	@IdPuesto			INT				=	NULL,
	@IdEstatus			INT				=	NULL,
	@Nombre				NVARCHAR(50)	=	NULL,
	@PrimerApellido		NVARCHAR(50)	=	NULL,
	@SegundoApellido	NVARCHAR(50)	=	NULL,
	@CURP				NVARCHAR(20)	=	NULL,
	@RFC				NVARCHAR(15)	=	NULL,
	@FechaNacimiento	DATE			=	NULL,
	@Correo				NVARCHAR(50)	=	NULL,
	@Telefono			NVARCHAR(15)	=	NULL,
	@FechaContratacion	DATE			=	NULL,
	@Especialidades		NVARCHAR(MAX)	=	NULL,	-- Ids separados por coma: "1,3,5" (solo para Entrenador)
	@UsuarioSesion		INT				=	NULL
AS
BEGIN
	SET NOCOUNT ON;

	-- Textos sin acentos, en mayusculas y sin espacios de mas (dbo.fnRemoverTildes)
	SET @Nombre = dbo.fnRemoverTildes(LTRIM(RTRIM(@Nombre)))
	SET @PrimerApellido = dbo.fnRemoverTildes(LTRIM(RTRIM(@PrimerApellido)))
	SET @SegundoApellido = dbo.fnRemoverTildes(LTRIM(RTRIM(@SegundoApellido)))
	SET @CURP = dbo.fnRemoverTildes(LTRIM(RTRIM(@CURP)))
	SET @RFC = dbo.fnRemoverTildes(LTRIM(RTRIM(@RFC)))

	-- REGISTRAR (1) O MODIFICAR (4): validaciones comunes
	IF @Accion IN (1, 4)
	BEGIN
		-- Identificación oficial: al menos CURP o RFC
		IF NULLIF(LTRIM(@CURP), '') IS NULL AND NULLIF(LTRIM(@RFC), '') IS NULL
		BEGIN
			SELECT 'Captura al menos la CURP o el RFC del empleado.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF @CURP IS NOT NULL AND EXISTS (SELECT 1 FROM EMPLEADO WHERE CURP = @CURP AND FechaBaja IS NULL AND IdEmpleado <> ISNULL(@IdEmpleado, 0))
		BEGIN
			SELECT 'Ya hay un empleado registrado con la CURP ' + @CURP + '.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF @RFC IS NOT NULL AND EXISTS (SELECT 1 FROM EMPLEADO WHERE RFC = @RFC AND FechaBaja IS NULL AND IdEmpleado <> ISNULL(@IdEmpleado, 0))
		BEGIN
			SELECT 'Ya hay un empleado registrado con el RFC ' + @RFC + '.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF EXISTS (SELECT 1 FROM EMPLEADO WHERE Correo = @Correo AND FechaBaja IS NULL AND IdEmpleado <> ISNULL(@IdEmpleado, 0))
		BEGIN
			SELECT 'El correo ' + @Correo + ' ya pertenece a otro empleado.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF NOT EXISTS (SELECT 1 FROM SUCURSAL WHERE IdSucursal = @IdSucursal AND IdEstatus = 1 AND FechaBaja IS NULL)
		BEGIN
			SELECT 'La sucursal seleccionada no está activa.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		-- Si deja de ser entrenador no puede tener clases futuras asignadas
		IF @Accion = 4
		   AND NOT EXISTS (SELECT 1 FROM PUESTO WHERE IdPuesto = @IdPuesto AND Nombre = 'Entrenador')
		   AND EXISTS (SELECT 1 FROM CLASE WHERE IdEmpleado = @IdEmpleado AND FechaBaja IS NULL AND Fecha >= CAST(GETDATE() AS DATE))
		BEGIN
			SELECT 'El empleado tiene clases programadas; reasígnalas antes de cambiarle el puesto.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		-- Tu propio empleado no puede pasar a Entrenador: su cuenta tomaría ese rol y perderías el de Administrador
		IF @Accion = 4
		   AND EXISTS (SELECT 1 FROM PUESTO WHERE IdPuesto = @IdPuesto AND Nombre = 'Entrenador')
		   AND EXISTS (SELECT 1 FROM EMPLEADO WHERE IdEmpleado = @IdEmpleado AND IdUsuario = @UsuarioSesion)
		BEGIN
			SELECT 'No puedes cambiar a Entrenador al empleado ligado a tu propia cuenta.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		-- Tu propio empleado no puede quedar inactivo: el login exige un empleado activo y te quedarías sin acceso
		IF @Accion = 4 AND @IdEstatus <> 1
		   AND EXISTS (SELECT 1 FROM EMPLEADO WHERE IdEmpleado = @IdEmpleado AND IdUsuario = @UsuarioSesion)
		BEGIN
			SELECT 'No puedes desactivar al empleado ligado a tu propia cuenta.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END
	END

	-- Especialidades: solo aplican si el puesto es Entrenador
	DECLARE @Esp TABLE (IdEspecialidad INT PRIMARY KEY)
	IF @Accion IN (1, 4) AND EXISTS (SELECT 1 FROM PUESTO WHERE IdPuesto = @IdPuesto AND Nombre = 'Entrenador')
		-- @Especialidades llega como "1,3,5": se toma cada especialidad cuyo Id aparece en la lista
		-- (se agregan comas a los lados para que el 1 no coincida con el 13)
		INSERT INTO @Esp (IdEspecialidad)
		SELECT IdEspecialidad
		FROM ESPECIALIDAD
		WHERE ',' + ISNULL(@Especialidades, '') + ',' LIKE '%,' + CAST(IdEspecialidad AS NVARCHAR(10)) + ',%'

	IF @Accion = 1
	BEGIN
		INSERT INTO EMPLEADO (IdSucursal, IdPuesto, IdUsuario, IdEstatus, Nombre, PrimerApellido, SegundoApellido, CURP, RFC,
							  FechaNacimiento, Correo, Telefono, FechaContratacion, FechaRegistro, UsuarioRegistro)
		VALUES (@IdSucursal, @IdPuesto, NULL, @IdEstatus, @Nombre, @PrimerApellido, @SegundoApellido, @CURP, @RFC,
				@FechaNacimiento, @Correo, @Telefono, @FechaContratacion, GETDATE(), @UsuarioSesion)

		DECLARE @Nuevo INT = SCOPE_IDENTITY()

		INSERT INTO EMPLEADO_ESPECIALIDAD (IdEspecialidad, IdEmpleado)
		SELECT IdEspecialidad, @Nuevo FROM @Esp

		SELECT 'Empleado registrado correctamente.' AS Mensaje, 'success' AS TipoMensaje
	END

	-- LEER UN EMPLEADO (sus especialidades se leen con la Accion 6)
	IF @Accion = 2
	BEGIN
		SELECT
			E.IdEmpleado, E.IdSucursal, E.IdPuesto, E.IdEstatus, E.Nombre, E.PrimerApellido, E.SegundoApellido,
			E.CURP, E.RFC, E.FechaNacimiento, E.Correo, E.Telefono, E.FechaContratacion
		FROM EMPLEADO E
		WHERE E.IdEmpleado = @IdEmpleado AND E.FechaBaja IS NULL
	END

	IF @Accion = 3
	BEGIN
		SELECT
			E.IdEmpleado,
			LTRIM(RTRIM(CONCAT(E.Nombre, ' ', E.PrimerApellido, ' ', ISNULL(E.SegundoApellido, '')))) AS Empleado,
			E.Correo,
			E.Telefono,
			E.CURP,
			E.RFC,
			E.FechaContratacion,
			P.Nombre		AS Puesto,
			S.Nombre		AS Sucursal,
			U.Cuenta,
			E.IdEstatus,
			ES.Descripcion	AS Estatus
		FROM EMPLEADO E
		INNER JOIN PUESTO P		ON P.IdPuesto = E.IdPuesto
		INNER JOIN SUCURSAL S	ON S.IdSucursal = E.IdSucursal
		INNER JOIN ESTATUS ES	ON ES.IdEstatus = E.IdEstatus
		LEFT JOIN USUARIO U		ON U.IdUsuario = E.IdUsuario AND U.FechaBaja IS NULL
		WHERE E.FechaBaja IS NULL
		  AND (@Nombre IS NULL OR CONCAT(E.Nombre, ' ', E.PrimerApellido, ' ', ISNULL(E.SegundoApellido, '')) LIKE '%' + @Nombre + '%')
		  AND (@IdSucursal IS NULL OR E.IdSucursal = @IdSucursal)
		  AND (@IdPuesto IS NULL OR E.IdPuesto = @IdPuesto)
		  AND (@IdEstatus IS NULL OR E.IdEstatus = @IdEstatus)
		ORDER BY E.Nombre, E.PrimerApellido
	END

	IF @Accion = 4
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM EMPLEADO WHERE IdEmpleado = @IdEmpleado AND FechaBaja IS NULL)
		BEGIN
			SELECT 'No se encontró el empleado a modificar.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		UPDATE EMPLEADO
		SET IdSucursal			= @IdSucursal,
			IdPuesto			= @IdPuesto,
			IdEstatus			= @IdEstatus,
			Nombre				= @Nombre,
			PrimerApellido		= @PrimerApellido,
			SegundoApellido		= @SegundoApellido,
			CURP				= @CURP,
			RFC					= @RFC,
			FechaNacimiento		= @FechaNacimiento,
			Correo				= @Correo,
			Telefono			= @Telefono,
			FechaContratacion	= @FechaContratacion,
			FechaModificacion	= GETDATE(),
			UsuarioModificacion	= @UsuarioSesion
		WHERE IdEmpleado = @IdEmpleado

		-- Se reemplazan las especialidades (queda vacío si ya no es entrenador)
		DELETE FROM EMPLEADO_ESPECIALIDAD WHERE IdEmpleado = @IdEmpleado
		INSERT INTO EMPLEADO_ESPECIALIDAD (IdEspecialidad, IdEmpleado)
		SELECT IdEspecialidad, @IdEmpleado FROM @Esp

		-- Puesto y rol de su cuenta (si tiene): el puesto Entrenador va con el rol Entrenador y viceversa
		--  - Pasa a Entrenador: su cuenta toma el rol Entrenador.
		--  - Deja de ser Entrenador: su cuenta queda Inactiva hasta que el administrador le asigne el nuevo rol en Usuarios.
		DECLARE @Aviso NVARCHAR(200) = ''
		DECLARE @IdRolEntrenador INT = (SELECT IdRol FROM ROL WHERE Descripcion = 'Entrenador')

		IF EXISTS (SELECT 1 FROM PUESTO WHERE IdPuesto = @IdPuesto AND Nombre = 'Entrenador')
		BEGIN
			UPDATE U
			SET IdRol				= @IdRolEntrenador,
				FechaModificacion	= GETDATE(),
				UsuarioModificacion	= @UsuarioSesion
			FROM USUARIO U
			INNER JOIN EMPLEADO E ON E.IdUsuario = U.IdUsuario
			WHERE E.IdEmpleado = @IdEmpleado AND U.FechaBaja IS NULL AND U.IdRol <> @IdRolEntrenador

			IF @@ROWCOUNT > 0
				SET @Aviso = ' Su cuenta ahora tiene el rol Entrenador.'
		END
		ELSE
		BEGIN
			UPDATE U
			SET IdEstatus			= 2,	-- Inactivo
				FechaModificacion	= GETDATE(),
				UsuarioModificacion	= @UsuarioSesion
			FROM USUARIO U
			INNER JOIN EMPLEADO E ON E.IdUsuario = U.IdUsuario
			WHERE E.IdEmpleado = @IdEmpleado AND U.FechaBaja IS NULL AND U.IdRol = @IdRolEntrenador

			IF @@ROWCOUNT > 0
				SET @Aviso = ' Su cuenta quedó inactiva porque ya no es Entrenador: asígnale su nuevo rol en Usuarios.'
		END

		SELECT 'Empleado actualizado correctamente.' + @Aviso AS Mensaje, 'success' AS TipoMensaje
	END

	-- ELIMINAR (baja lógica; su cuenta de usuario también se da de baja para respetar la relación 1:1)
	IF @Accion = 5
	BEGIN
		IF EXISTS (SELECT 1 FROM EMPLEADO WHERE IdEmpleado = @IdEmpleado AND IdUsuario = @UsuarioSesion)
		BEGIN
			SELECT 'No puedes eliminar al empleado ligado a tu propia cuenta.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF EXISTS (SELECT 1 FROM CLASE WHERE IdEmpleado = @IdEmpleado AND FechaBaja IS NULL AND Fecha >= CAST(GETDATE() AS DATE))
		BEGIN
			SELECT 'No se puede eliminar: el empleado tiene clases programadas.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		DECLARE @IdUsuario INT = (SELECT IdUsuario FROM EMPLEADO WHERE IdEmpleado = @IdEmpleado AND FechaBaja IS NULL)

		UPDATE EMPLEADO
		SET FechaBaja = GETDATE(), UsuarioBaja = @UsuarioSesion, IdEstatus = 2, IdUsuario = NULL
		WHERE IdEmpleado = @IdEmpleado AND FechaBaja IS NULL

		IF @@ROWCOUNT = 0
		BEGIN
			SELECT 'No se encontró el empleado a eliminar.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF @IdUsuario IS NOT NULL
			UPDATE USUARIO
			SET FechaBaja = GETDATE(), UsuarioBaja = @UsuarioSesion, IdEstatus = 2
			WHERE IdUsuario = @IdUsuario AND FechaBaja IS NULL

		SELECT 'Empleado eliminado correctamente' + CASE WHEN @IdUsuario IS NOT NULL THEN ' (su cuenta de usuario también se dio de baja).' ELSE '.' END AS Mensaje,
			   'success' AS TipoMensaje
	END

	-- ESPECIALIDADES DE LOS EMPLEADOS (un renglón por especialidad; @IdEmpleado NULL = de todos).
	-- C# las junta con foreach para mostrarlas como "Pesas, Yoga"
	IF @Accion = 6
	BEGIN
		SELECT EE.IdEmpleado, EE.IdEspecialidad, ESP.Nombre
		FROM EMPLEADO_ESPECIALIDAD EE
		INNER JOIN ESPECIALIDAD ESP ON ESP.IdEspecialidad = EE.IdEspecialidad
		INNER JOIN EMPLEADO E		ON E.IdEmpleado = EE.IdEmpleado
		WHERE E.FechaBaja IS NULL
		  AND (@IdEmpleado IS NULL OR EE.IdEmpleado = @IdEmpleado)
		ORDER BY ESP.Nombre
	END

	-- COMBO DE ENTRENADORES ACTIVOS (opcional por sucursal) (C# le agrega sus especialidades)
	IF @Accion = 8
	BEGIN
		SELECT
			E.IdEmpleado,
			LTRIM(RTRIM(CONCAT(E.Nombre, ' ', E.PrimerApellido, ' ', ISNULL(E.SegundoApellido, '')))) AS Entrenador,
			E.IdSucursal
		FROM EMPLEADO E
		INNER JOIN PUESTO P ON P.IdPuesto = E.IdPuesto
		WHERE E.IdEstatus = 1
		  AND E.FechaBaja IS NULL
		  AND P.Nombre = 'Entrenador'
		  AND (@IdSucursal IS NULL OR E.IdSucursal = @IdSucursal)
		ORDER BY E.Nombre, E.PrimerApellido
	END
END
GO

CREATE OR ALTER PROCEDURE [dbo].[spClase]
	@Accion				INT				=	NULL,
	@IdClase			INT				=	NULL,
	@IdEstatus			INT				=	NULL,
	@Nombre				NVARCHAR(50)	=	NULL,
	@Fecha				DATE			=	NULL,
	@FechaHasta			DATE			=	NULL,	-- Solo para la consulta (rango)
	@HoraInicio			TIME			=	NULL,
	@HoraFin			TIME			=	NULL,
	@Salon				NVARCHAR(50)	=	NULL,
	@CupoMaximo			INT				=	NULL,
	@IdEmpleado			INT				=	NULL,
	@IdSucursal			INT				=	NULL,
	@UsuarioSesion		INT				=	NULL
AS
BEGIN
	SET NOCOUNT ON;

	-- Textos sin acentos, en mayusculas y sin espacios de mas (dbo.fnRemoverTildes)
	SET @Nombre = dbo.fnRemoverTildes(LTRIM(RTRIM(@Nombre)))
	SET @Salon = dbo.fnRemoverTildes(LTRIM(RTRIM(@Salon)))

	-- REGISTRAR (1) O MODIFICAR (4): validaciones de agenda
	IF @Accion IN (1, 4)
	BEGIN
		IF DATEDIFF(DAY, '19000101', @Fecha) % 7 > 4
		BEGIN
			SELECT 'Las clases grupales solo se programan de lunes a viernes.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF @Fecha < CAST(GETDATE() AS DATE)
		BEGIN
			SELECT 'No se pueden programar clases en fechas pasadas.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF @HoraFin <= @HoraInicio
		BEGIN
			SELECT 'La hora de fin debe ser posterior a la hora de inicio.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		DECLARE @Apertura TIME, @Cierre TIME, @Aforo INT
		SELECT @Apertura = HoraApertura, @Cierre = HoraCierre, @Aforo = AforoMaximo
		FROM SUCURSAL WHERE IdSucursal = @IdSucursal AND IdEstatus = 1 AND FechaBaja IS NULL

		IF @Aforo IS NULL
		BEGIN
			SELECT 'La sucursal seleccionada no está activa.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF @HoraInicio < @Apertura OR @HoraFin > @Cierre
		BEGIN
			SELECT 'La clase debe estar dentro del horario de la sucursal (' + LEFT(CAST(@Apertura AS NVARCHAR(8)), 5) + ' - '
				   + LEFT(CAST(@Cierre AS NVARCHAR(8)), 5) + ').' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF ISNULL(@CupoMaximo, 0) <= 0 OR @CupoMaximo > @Aforo
		BEGIN
			SELECT 'El cupo debe ser mayor a 0 y no puede superar el aforo de la sucursal (' + CAST(@Aforo AS NVARCHAR(10)) + ').' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF NOT EXISTS (SELECT 1 FROM EMPLEADO E INNER JOIN PUESTO P ON P.IdPuesto = E.IdPuesto
					   WHERE E.IdEmpleado = @IdEmpleado AND E.IdEstatus = 1 AND E.FechaBaja IS NULL AND P.Nombre = 'Entrenador')
		BEGIN
			SELECT 'El entrenador seleccionado no está activo o no tiene el puesto de Entrenador.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		-- Choques de horario (dos rangos se enciman si InicioA < FinB y InicioB < FinA)
		IF EXISTS (SELECT 1 FROM CLASE WHERE FechaBaja IS NULL AND IdClase <> ISNULL(@IdClase, 0)
				   AND IdEmpleado = @IdEmpleado AND Fecha = @Fecha AND HoraInicio < @HoraFin AND @HoraInicio < HoraFin)
		BEGIN
			SELECT 'El entrenador ya tiene otra clase en ese horario.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF EXISTS (SELECT 1 FROM CLASE WHERE FechaBaja IS NULL AND IdClase <> ISNULL(@IdClase, 0)
				   AND IdSucursal = @IdSucursal AND Salon = @Salon AND Fecha = @Fecha AND HoraInicio < @HoraFin AND @HoraInicio < HoraFin)
		BEGIN
			SELECT 'El salón "' + @Salon + '" ya está ocupado en ese horario.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		-- Al editar, el cupo no puede quedar por debajo de las reservas confirmadas
		IF @Accion = 4 AND @CupoMaximo < (SELECT COUNT(*) FROM RESERVA WHERE IdClase = @IdClase AND IdEstatus IN (5, 6, 7) AND FechaBaja IS NULL)
		BEGIN
			SELECT 'El cupo no puede ser menor a las reservas ya confirmadas.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END
	END

	IF @Accion = 1
	BEGIN
		INSERT INTO CLASE (IdEstatus, Nombre, Fecha, HoraInicio, HoraFin, Salon, CupoMaximo, IdEmpleado, IdSucursal, FechaRegistro, UsuarioRegistro)
		VALUES (1, @Nombre, @Fecha, @HoraInicio, @HoraFin, @Salon, @CupoMaximo, @IdEmpleado, @IdSucursal, GETDATE(), @UsuarioSesion)

		SELECT 'Clase programada correctamente.' AS Mensaje, 'success' AS TipoMensaje
	END

	IF @Accion = 2
	BEGIN
		SELECT IdClase, IdEstatus, Nombre, Fecha, HoraInicio, HoraFin, Salon, CupoMaximo, IdEmpleado, IdSucursal
		FROM CLASE
		WHERE IdClase = @IdClase AND FechaBaja IS NULL
	END

	-- AGENDA (rango de fechas, sucursal y entrenador opcionales)
	IF @Accion = 3
	BEGIN
		SELECT
			CL.IdClase,
			CL.Nombre,
			CL.Fecha,
			DATEDIFF(DAY, '19000101', CL.Fecha) % 7 AS DiaSemana,	-- 0 = lunes
			CL.HoraInicio,
			CL.HoraFin,
			CL.Salon,
			CL.CupoMaximo,
			S.Nombre AS Sucursal,
			LTRIM(RTRIM(CONCAT(E.Nombre, ' ', E.PrimerApellido))) AS Entrenador,
			(SELECT COUNT(*) FROM RESERVA R WHERE R.IdClase = CL.IdClase AND R.IdEstatus IN (5, 6, 7) AND R.FechaBaja IS NULL) AS Reservadas
		FROM CLASE CL
		INNER JOIN SUCURSAL S	ON S.IdSucursal = CL.IdSucursal
		INNER JOIN EMPLEADO E	ON E.IdEmpleado = CL.IdEmpleado
		WHERE CL.FechaBaja IS NULL
		  AND CL.Fecha BETWEEN @Fecha AND @FechaHasta
		  AND (@IdSucursal IS NULL OR CL.IdSucursal = @IdSucursal)
		  AND (@IdEmpleado IS NULL OR CL.IdEmpleado = @IdEmpleado)
		  AND (@Nombre IS NULL OR CL.Nombre LIKE '%' + @Nombre + '%')
		ORDER BY CL.Fecha, CL.HoraInicio
	END

	IF @Accion = 4
	BEGIN
		UPDATE CLASE
		SET Nombre				= @Nombre,
			Fecha				= @Fecha,
			HoraInicio			= @HoraInicio,
			HoraFin				= @HoraFin,
			Salon				= @Salon,
			CupoMaximo			= @CupoMaximo,
			IdEmpleado			= @IdEmpleado,
			IdSucursal			= @IdSucursal,
			FechaModificacion	= GETDATE(),
			UsuarioModificacion	= @UsuarioSesion
		WHERE IdClase = @IdClase AND FechaBaja IS NULL

		IF @@ROWCOUNT = 0
			SELECT 'No se encontró la clase a modificar.' AS Mensaje, 'warning' AS TipoMensaje
		ELSE
			SELECT 'Clase actualizada correctamente.' AS Mensaje, 'success' AS TipoMensaje
	END

	-- CANCELAR CLASE (baja lógica; sus reservas confirmadas se cancelan)
	IF @Accion = 5
	BEGIN
		UPDATE CLASE
		SET FechaBaja = GETDATE(), UsuarioBaja = @UsuarioSesion, IdEstatus = 4
		WHERE IdClase = @IdClase AND FechaBaja IS NULL

		IF @@ROWCOUNT = 0
		BEGIN
			SELECT 'No se encontró la clase a cancelar.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		UPDATE RESERVA
		SET IdEstatus = 4, FechaModificacion = GETDATE(), UsuarioModificacion = @UsuarioSesion
		WHERE IdClase = @IdClase AND IdEstatus = 5 AND FechaBaja IS NULL

		SELECT 'Clase cancelada correctamente (sus reservas también se cancelaron).' AS Mensaje, 'success' AS TipoMensaje
	END
END
GO

CREATE OR ALTER PROCEDURE [dbo].[spRutina]
	@Accion				INT				=	NULL,
	@IdRutina			INT				=	NULL,
	@IdEmpleado			INT				=	NULL,	-- Entrenador que la diseña
	@IdCliente			INT				=	NULL,	-- Socio asignado
	@Nombre				NVARCHAR(50)	=	NULL,
	@Objetivo			NVARCHAR(50)	=	NULL,
	@FechaAsignacion	DATE			=	NULL,
	@IdEjercicio		INT				=	NULL,	-- Detalle: un ejercicio a la vez (Accion 10)
	@DiaSemana			NVARCHAR(50)	=	NULL,
	@Series				INT				=	NULL,
	@Repeticiones		INT				=	NULL,
	@TiempoDescanso		INT				=	NULL,
	@SoloValidar		BIT				=	0,		-- 1 = solo revisa las reglas, no guarda
	@UsuarioSesion		INT				=	NULL	-- Usuario que realiza la acción (auditoría)
AS
BEGIN
	SET NOCOUNT ON;

	-- Textos sin acentos, en mayusculas y sin espacios de mas (dbo.fnRemoverTildes)
	SET @Nombre = dbo.fnRemoverTildes(LTRIM(RTRIM(@Nombre)))

	-- REGISTRAR LA CABECERA DE LA RUTINA (los ejercicios se agregan con la Accion 10)
	IF @Accion = 1
	BEGIN
		-- El socio debe estar activo
		IF NOT EXISTS (SELECT 1 FROM CLIENTE WHERE IdCliente = @IdCliente AND IdEstatus = 1 AND FechaBaja IS NULL)
		BEGIN
			SELECT 'El socio seleccionado no está activo.' AS Mensaje, 'warning' AS TipoMensaje, 0 AS IdRutina
			RETURN
		END

		-- Expediente médico obligatorio para asignar rutinas
		IF NOT EXISTS (SELECT 1 FROM EXPEDIENTE_MEDICO WHERE IdCliente = @IdCliente)
		BEGIN
			SELECT 'El socio no tiene expediente médico. Regístralo antes de asignarle una rutina.' AS Mensaje, 'warning' AS TipoMensaje, 0 AS IdRutina
			RETURN
		END

		-- El entrenador debe ser un empleado activo con puesto Entrenador
		IF NOT EXISTS (SELECT 1 FROM EMPLEADO E INNER JOIN PUESTO P ON P.IdPuesto = E.IdPuesto
					   WHERE E.IdEmpleado = @IdEmpleado AND E.IdEstatus = 1 AND E.FechaBaja IS NULL AND P.Nombre = 'Entrenador')
		BEGIN
			SELECT 'El entrenador seleccionado no está activo o no tiene el puesto de Entrenador.' AS Mensaje, 'warning' AS TipoMensaje, 0 AS IdRutina
			RETURN
		END

		-- Los ejercicios se validan y guardan uno por uno con la Accion 10
		IF @SoloValidar = 1
		BEGIN
			SELECT 'Correcto.' AS Mensaje, 'success' AS TipoMensaje, 0 AS IdRutina
			RETURN
		END

		INSERT INTO RUTINA (IdEmpleado, IdCliente, IdEstatus, Nombre, FechaAsignacion, Objetivo, FechaRegistro, UsuarioRegistro)
		VALUES (@IdEmpleado, @IdCliente, 1, @Nombre, @FechaAsignacion, @Objetivo, GETDATE(), @UsuarioSesion)

		SELECT 'Rutina asignada correctamente.' AS Mensaje, 'success' AS TipoMensaje, SCOPE_IDENTITY() AS IdRutina
	END

	-- LEER LA CABECERA DE UNA RUTINA
	IF @Accion = 2
	BEGIN
		SELECT
			R.IdRutina,
			R.Nombre,
			R.Objetivo,
			R.FechaAsignacion,
			R.IdCliente,
			C.Folio,
			LTRIM(RTRIM(CONCAT(C.Nombre, ' ', C.PrimerApellido, ' ', ISNULL(C.SegundoApellido, '')))) AS Socio,
			R.IdEmpleado,
			LTRIM(RTRIM(CONCAT(E.Nombre, ' ', E.PrimerApellido, ' ', ISNULL(E.SegundoApellido, '')))) AS Entrenador,
			ES.Descripcion		AS Estatus
		FROM RUTINA R
		INNER JOIN CLIENTE C	ON C.IdCliente = R.IdCliente
		INNER JOIN EMPLEADO E	ON E.IdEmpleado = R.IdEmpleado
		INNER JOIN ESTATUS ES	ON ES.IdEstatus = R.IdEstatus
		WHERE R.IdRutina = @IdRutina
		  AND R.FechaBaja IS NULL
	END

	-- CONSULTA GENERAL (filtros opcionales por socio, entrenador y objetivo)
	IF @Accion = 3
	BEGIN
		SELECT
			R.IdRutina,
			R.Nombre,
			R.Objetivo,
			R.FechaAsignacion,
			C.Folio,
			LTRIM(RTRIM(CONCAT(C.Nombre, ' ', C.PrimerApellido, ' ', ISNULL(C.SegundoApellido, '')))) AS Socio,
			LTRIM(RTRIM(CONCAT(E.Nombre, ' ', E.PrimerApellido, ' ', ISNULL(E.SegundoApellido, '')))) AS Entrenador,
			(SELECT COUNT(*) FROM DETALLE_RUTINA D WHERE D.IdRutina = R.IdRutina)					AS TotalEjercicios,
			(SELECT COUNT(DISTINCT D.DiaSemana) FROM DETALLE_RUTINA D WHERE D.IdRutina = R.IdRutina)	AS TotalDias
		FROM RUTINA R
		INNER JOIN CLIENTE C	ON C.IdCliente = R.IdCliente
		INNER JOIN EMPLEADO E	ON E.IdEmpleado = R.IdEmpleado
		WHERE R.FechaBaja IS NULL
		  AND (@IdCliente IS NULL OR R.IdCliente = @IdCliente)
		  AND (@IdEmpleado IS NULL OR R.IdEmpleado = @IdEmpleado)
		  AND (@Objetivo IS NULL OR R.Objetivo = @Objetivo)
		ORDER BY R.FechaAsignacion DESC, R.IdRutina DESC
	END

	-- ELIMINAR (baja lógica)
	IF @Accion = 5
	BEGIN
		UPDATE RUTINA
		SET FechaBaja	= GETDATE(),
			UsuarioBaja	= @UsuarioSesion,
			IdEstatus	= 2		-- Inactivo
		WHERE IdRutina = @IdRutina
		  AND FechaBaja IS NULL

		IF @@ROWCOUNT = 0
			SELECT 'No se encontró la rutina a eliminar.' AS Mensaje, 'warning' AS TipoMensaje
		ELSE
			SELECT 'Rutina eliminada correctamente.' AS Mensaje, 'success' AS TipoMensaje
	END

	-- DETALLE DE UNA RUTINA (ordenado de Lunes a Domingo)
	IF @Accion = 6
	BEGIN
		SELECT
			D.IdDetalleRutina,
			D.DiaSemana,
			D.IdEjercicio,
			EJ.Nombre			AS Ejercicio,
			EJ.GrupoMuscular,
			EJ.NivelDificultad,
			EJ.ContraIndicacion,
			D.Series,
			D.Repeticiones,
			D.TiempoDescanso
		FROM DETALLE_RUTINA D
		INNER JOIN EJERCICIO EJ ON EJ.IdEjercicio = D.IdEjercicio
		WHERE D.IdRutina = @IdRutina
		ORDER BY CASE D.DiaSemana
					WHEN 'Lunes' THEN 1 WHEN 'Martes' THEN 2 WHEN 'Miércoles' THEN 3 WHEN 'Jueves' THEN 4
					WHEN 'Viernes' THEN 5 WHEN 'Sábado' THEN 6 ELSE 7 END,
				 D.IdDetalleRutina
	END

	-- COMBO DE SOCIOS ACTIVOS (indica si ya tienen expediente médico)
	IF @Accion = 7
	BEGIN
		SELECT
			C.IdCliente,
			CONCAT(C.Folio, ' - ', LTRIM(RTRIM(CONCAT(C.Nombre, ' ', C.PrimerApellido, ' ', ISNULL(C.SegundoApellido, ''))))) AS Socio,
			CASE WHEN EXISTS (SELECT 1 FROM EXPEDIENTE_MEDICO EM WHERE EM.IdCliente = C.IdCliente) THEN 1 ELSE 0 END AS TieneExpediente
		FROM CLIENTE C
		WHERE C.IdEstatus = 1
		  AND C.FechaBaja IS NULL
		ORDER BY C.Nombre, C.PrimerApellido
	END

	-- COMBO DE ENTRENADORES (empleados activos con puesto Entrenador)
	IF @Accion = 8
	BEGIN
		SELECT
			E.IdEmpleado,
			LTRIM(RTRIM(CONCAT(E.Nombre, ' ', E.PrimerApellido, ' ', ISNULL(E.SegundoApellido, '')))) AS Entrenador
		FROM EMPLEADO E
		INNER JOIN PUESTO P ON P.IdPuesto = E.IdPuesto
		WHERE E.IdEstatus = 1
		  AND E.FechaBaja IS NULL
		  AND P.Nombre = 'Entrenador'
		ORDER BY E.Nombre, E.PrimerApellido
	END

	-- EXPEDIENTE MÉDICO DEL SOCIO (para las alertas de contraindicaciones)
	IF @Accion = 9
	BEGIN
		SELECT TOP 1
			IdExpediente,
			TipoSangre,
			PadecimientosCronicos,
			LesionesPrevias,
			Alergias,
			CondicionRiesgo
		FROM EXPEDIENTE_MEDICO
		WHERE IdCliente = @IdCliente
	END

	-- AGREGAR UN EJERCICIO AL DETALLE DE LA RUTINA (la página lo llama una vez por ejercicio).
	-- Con @SoloValidar = 1 solo revisa las reglas: así se validan todos antes de guardar la rutina.
	IF @Accion = 10
	BEGIN
		-- Matriz semanal: solo días de Lunes a Domingo
		IF @DiaSemana IS NULL OR @DiaSemana NOT IN ('Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado', 'Domingo')
		BEGIN
			SELECT 'Hay ejercicios con un día de la semana no válido.' AS Mensaje, 'warning' AS TipoMensaje, 0 AS IdRutina
			RETURN
		END

		-- Métricas: Series > 0, Repeticiones > 0, TiempoDescanso >= 0
		IF ISNULL(@Series, 0) <= 0 OR ISNULL(@Repeticiones, 0) <= 0 OR @TiempoDescanso IS NULL OR @TiempoDescanso < 0
		BEGIN
			SELECT 'Las series y repeticiones deben ser mayores a 0 y el descanso no puede ser negativo.' AS Mensaje, 'warning' AS TipoMensaje, 0 AS IdRutina
			RETURN
		END

		-- El ejercicio debe existir y estar activo
		IF NOT EXISTS (SELECT 1 FROM EJERCICIO WHERE IdEjercicio = @IdEjercicio AND IdEstatus = 1 AND FechaBaja IS NULL)
		BEGIN
			SELECT 'Hay ejercicios que no existen o están inactivos.' AS Mensaje, 'warning' AS TipoMensaje, 0 AS IdRutina
			RETURN
		END

		IF @SoloValidar = 1
		BEGIN
			SELECT 'Correcto.' AS Mensaje, 'success' AS TipoMensaje, 0 AS IdRutina
			RETURN
		END

		INSERT INTO DETALLE_RUTINA (IdRutina, IdEjercicio, DiaSemana, Series, Repeticiones, TiempoDescanso)
		VALUES (@IdRutina, @IdEjercicio, @DiaSemana, @Series, @Repeticiones, @TiempoDescanso)

		SELECT 'Ejercicio agregado.' AS Mensaje, 'success' AS TipoMensaje, @IdRutina AS IdRutina
	END
END

GO

CREATE OR ALTER PROCEDURE [dbo].[spExpedienteMedico]
	@Accion					INT				=	NULL,
	@IdCliente				INT				=	NULL,
	@Busqueda				NVARCHAR(50)	=	NULL,
	@TipoSangre				NVARCHAR(50)	=	NULL,
	@PadecimientosCronicos	NVARCHAR(50)	=	NULL,
	@LesionesPrevias		NVARCHAR(50)	=	NULL,
	@Alergias				NVARCHAR(50)	=	NULL,
	@CondicionRiesgo		NVARCHAR(50)	=	NULL,
	@UsuarioSesion			INT				=	NULL
AS
BEGIN
	SET NOCOUNT ON;

	-- Textos sin acentos, en mayusculas y sin espacios de mas (dbo.fnRemoverTildes)
	SET @Busqueda = dbo.fnRemoverTildes(LTRIM(RTRIM(@Busqueda)))
	SET @PadecimientosCronicos = dbo.fnRemoverTildes(LTRIM(RTRIM(@PadecimientosCronicos)))
	SET @LesionesPrevias = dbo.fnRemoverTildes(LTRIM(RTRIM(@LesionesPrevias)))
	SET @Alergias = dbo.fnRemoverTildes(LTRIM(RTRIM(@Alergias)))
	SET @CondicionRiesgo = dbo.fnRemoverTildes(LTRIM(RTRIM(@CondicionRiesgo)))

	-- GUARDAR (registra o actualiza: un socio tiene un solo expediente)
	IF @Accion = 1
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM CLIENTE WHERE IdCliente = @IdCliente AND FechaBaja IS NULL)
		BEGIN
			SELECT 'No se encontró el socio.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF EXISTS (SELECT 1 FROM EXPEDIENTE_MEDICO WHERE IdCliente = @IdCliente)
		BEGIN
			UPDATE EXPEDIENTE_MEDICO
			SET TipoSangre = @TipoSangre, PadecimientosCronicos = @PadecimientosCronicos, LesionesPrevias = @LesionesPrevias,
				Alergias = @Alergias, CondicionRiesgo = @CondicionRiesgo
			WHERE IdCliente = @IdCliente

			SELECT 'Expediente médico actualizado correctamente.' AS Mensaje, 'success' AS TipoMensaje
		END
		ELSE
		BEGIN
			INSERT INTO EXPEDIENTE_MEDICO (IdCliente, TipoSangre, PadecimientosCronicos, LesionesPrevias, Alergias, CondicionRiesgo)
			VALUES (@IdCliente, @TipoSangre, @PadecimientosCronicos, @LesionesPrevias, @Alergias, @CondicionRiesgo)

			SELECT 'Expediente médico registrado correctamente.' AS Mensaje, 'success' AS TipoMensaje
		END
	END

	-- LEER EL EXPEDIENTE DE UN SOCIO (con sus datos generales; columnas del expediente en NULL si no tiene)
	IF @Accion = 2
	BEGIN
		SELECT
			C.IdCliente,
			C.Folio,
			LTRIM(RTRIM(CONCAT(C.Nombre, ' ', C.PrimerApellido, ' ', ISNULL(C.SegundoApellido, '')))) AS Socio,
			C.FechaNacimiento,
			DATEDIFF(YEAR, C.FechaNacimiento, GETDATE())
				- CASE WHEN DATEADD(YEAR, DATEDIFF(YEAR, C.FechaNacimiento, GETDATE()), C.FechaNacimiento) > GETDATE() THEN 1 ELSE 0 END AS Edad,
			C.Telefono,
			EM.IdExpediente,
			EM.TipoSangre,
			EM.PadecimientosCronicos,
			EM.LesionesPrevias,
			EM.Alergias,
			EM.CondicionRiesgo
		FROM CLIENTE C
		LEFT JOIN EXPEDIENTE_MEDICO EM ON EM.IdCliente = C.IdCliente
		WHERE C.IdCliente = @IdCliente AND C.FechaBaja IS NULL
	END

	-- LISTA DE SOCIOS CON SU EXPEDIENTE (búsqueda por folio o nombre)
	IF @Accion = 3
	BEGIN
		SELECT
			C.IdCliente,
			C.Folio,
			LTRIM(RTRIM(CONCAT(C.Nombre, ' ', C.PrimerApellido, ' ', ISNULL(C.SegundoApellido, '')))) AS Socio,
			EM.TipoSangre,
			EM.PadecimientosCronicos,
			EM.LesionesPrevias,
			EM.CondicionRiesgo,
			CASE WHEN EM.IdExpediente IS NULL THEN 0 ELSE 1 END AS TieneExpediente
		FROM CLIENTE C
		LEFT JOIN EXPEDIENTE_MEDICO EM ON EM.IdCliente = C.IdCliente
		WHERE C.FechaBaja IS NULL
		  AND (@Busqueda IS NULL
			   OR CAST(C.Folio AS NVARCHAR(10)) LIKE @Busqueda + '%'
			   OR CONCAT(C.Nombre, ' ', C.PrimerApellido, ' ', ISNULL(C.SegundoApellido, '')) LIKE '%' + @Busqueda + '%')
		ORDER BY C.Folio DESC
	END
END
GO

CREATE OR ALTER PROCEDURE [dbo].[spReporte]
	@Accion				INT				=	NULL,
	@IdSucursal			INT				=	NULL,
	@IdPuesto			INT				=	NULL,
	@IdEspecialidad		INT				=	NULL,
	@IdTipoMembresia	INT				=	NULL,
	@IdEstatus			INT				=	NULL,
	@IdEmpleado			INT				=	NULL,
	@Busqueda			NVARCHAR(50)	=	NULL,	-- Folio / nombre del socio, o nombre de la clase
	@Objetivo			NVARCHAR(50)	=	NULL,
	@GrupoMuscular		NVARCHAR(50)	=	NULL,
	@NivelDificultad	NVARCHAR(50)	=	NULL,
	@FechaDesde			DATE			=	NULL,
	@FechaHasta			DATE			=	NULL
AS
BEGIN
	SET NOCOUNT ON;

	-- Textos sin acentos, en mayusculas y sin espacios de mas (dbo.fnRemoverTildes)
	SET @Busqueda = dbo.fnRemoverTildes(LTRIM(RTRIM(@Busqueda)))

	-- 1. EMPLEADOS Y DISTRIBUCIÓN (alfabético, agrupado por sede)
	IF @Accion = 1
	BEGIN
		SELECT
			S.Nombre		AS Sucursal,
			LTRIM(RTRIM(CONCAT(E.PrimerApellido, ' ', ISNULL(E.SegundoApellido, ''), ' ', E.Nombre))) AS Empleado,
			P.Nombre		AS Puesto,
			E.IdEmpleado,
			E.Correo,
			E.Telefono,
			E.FechaContratacion,
			ES.Descripcion	AS Estatus
		FROM EMPLEADO E
		INNER JOIN SUCURSAL S	ON S.IdSucursal = E.IdSucursal
		INNER JOIN PUESTO P		ON P.IdPuesto = E.IdPuesto
		INNER JOIN ESTATUS ES	ON ES.IdEstatus = E.IdEstatus
		WHERE E.FechaBaja IS NULL
		  AND (@IdSucursal IS NULL OR E.IdSucursal = @IdSucursal)
		  AND (@IdPuesto IS NULL OR E.IdPuesto = @IdPuesto)
		  AND (@IdEspecialidad IS NULL OR EXISTS (SELECT 1 FROM EMPLEADO_ESPECIALIDAD EE WHERE EE.IdEmpleado = E.IdEmpleado AND EE.IdEspecialidad = @IdEspecialidad))
		  AND (@FechaDesde IS NULL OR E.FechaContratacion >= @FechaDesde)
		  AND (@FechaHasta IS NULL OR E.FechaContratacion <= @FechaHasta)
		ORDER BY S.Nombre, E.PrimerApellido, E.SegundoApellido, E.Nombre
	END

	-- 2. SOCIOS Y ESTADO DE MEMBRESÍAS (última suscripción de cada socio; @IdEstatus = 0 -> sin membresía)
	IF @Accion = 2
	BEGIN
		EXEC spVencerSuscripciones

		SELECT
			C.IdCliente,
			C.Folio,
			LTRIM(RTRIM(CONCAT(C.Nombre, ' ', C.PrimerApellido, ' ', ISNULL(C.SegundoApellido, '')))) AS Socio,
			C.Correo,
			C.Telefono,
			S.Nombre			AS Sucursal,
			U.Membresia,
			U.FechaVencimiento,
			ISNULL(U.Estatus, 'Sin membresía') AS EstatusMembresia,
			ISNULL(U.IdEstatus, 0)				AS IdEstatusMembresia,
			CASE WHEN EXISTS (SELECT 1 FROM EXPEDIENTE_MEDICO EM WHERE EM.IdCliente = C.IdCliente) THEN 1 ELSE 0 END AS TieneExpediente
		FROM CLIENTE C
		INNER JOIN SUCURSAL S ON S.IdSucursal = C.IdSucursal
		OUTER APPLY (SELECT TOP 1 TM.IdTipoMembresia, TM.Nombre AS Membresia, SU.FechaVencimiento, SU.IdEstatus, ES.Descripcion AS Estatus
					 FROM SUSCRIPCION SU
					 INNER JOIN TIPO_MEMBRESIA TM	ON TM.IdTipoMembresia = SU.IdTipoMembresia
					 INNER JOIN ESTATUS ES			ON ES.IdEstatus = SU.IdEstatus
					 WHERE SU.IdCliente = C.IdCliente AND SU.FechaBaja IS NULL
					 ORDER BY CASE WHEN SU.IdEstatus = 1 THEN 0 ELSE 1 END, SU.FechaVencimiento DESC) U
		WHERE C.FechaBaja IS NULL
		  AND (@IdSucursal IS NULL OR C.IdSucursal = @IdSucursal)
		  AND (@IdTipoMembresia IS NULL OR U.IdTipoMembresia = @IdTipoMembresia)
		  AND (@IdEstatus IS NULL OR ISNULL(U.IdEstatus, 0) = @IdEstatus)
		ORDER BY C.Folio
	END

	-- 3. CONTROL DE ACCESO (minutos de permanencia; si no hay salida se calculan hasta el momento)
	IF @Accion = 3
	BEGIN
		SELECT
			C.Folio,
			LTRIM(RTRIM(CONCAT(C.Nombre, ' ', C.PrimerApellido, ' ', ISNULL(C.SegundoApellido, '')))) AS Socio,
			S.Nombre AS Sucursal,
			CA.FechaEntrada,
			CA.FechaSalida,
			DATEDIFF(MINUTE, CA.FechaEntrada, ISNULL(CA.FechaSalida, GETDATE())) AS Minutos,	-- Sin salida: minutos hasta ahora
			CASE WHEN CA.FechaSalida IS NULL THEN 1 ELSE 0 END AS EnCurso
		FROM CONTROL_ACCESO CA
		INNER JOIN CLIENTE C	ON C.IdCliente = CA.IdCliente
		INNER JOIN SUCURSAL S	ON S.IdSucursal = CA.IdSucursal
		WHERE (@IdSucursal IS NULL OR CA.IdSucursal = @IdSucursal)
		  AND (@FechaDesde IS NULL OR CA.FechaEntrada >= @FechaDesde)
		  AND (@FechaHasta IS NULL OR CA.FechaEntrada < DATEADD(DAY, 1, @FechaHasta))
		  AND (@Busqueda IS NULL
			   OR CAST(C.Folio AS NVARCHAR(10)) LIKE @Busqueda + '%'
			   OR CONCAT(C.Nombre, ' ', C.PrimerApellido, ' ', ISNULL(C.SegundoApellido, '')) LIKE '%' + @Busqueda + '%')
		ORDER BY CA.FechaEntrada DESC
	END

	-- 4. CLASES GRUPALES Y ASISTENCIA (ocupación y efectividad)
	IF @Accion = 4
	BEGIN
		SELECT
			X.*,
			CAST(ROUND(X.Reservas * 100.0 / NULLIF(X.CupoMaximo, 0), 1) AS DECIMAL(5,1))			AS Ocupacion,
			CAST(ROUND(X.Asistieron * 100.0 / NULLIF(X.Asistieron + X.Faltas, 0), 1) AS DECIMAL(5,1))	AS Efectividad
		FROM (
			SELECT
				CL.IdClase,
				CL.Nombre		AS Clase,
				CL.Fecha,
				CL.HoraInicio,
				CL.Salon,
				S.Nombre		AS Sucursal,
				LTRIM(RTRIM(CONCAT(E.Nombre, ' ', E.PrimerApellido))) AS Entrenador,
				CL.CupoMaximo,
				(SELECT COUNT(*) FROM RESERVA R WHERE R.IdClase = CL.IdClase AND R.FechaBaja IS NULL AND R.IdEstatus IN (5, 6, 7)) AS Reservas,
				(SELECT COUNT(*) FROM RESERVA R WHERE R.IdClase = CL.IdClase AND R.FechaBaja IS NULL AND R.IdEstatus = 6) AS Asistieron,
				(SELECT COUNT(*) FROM RESERVA R WHERE R.IdClase = CL.IdClase AND R.FechaBaja IS NULL AND R.IdEstatus = 7) AS Faltas
			FROM CLASE CL
			INNER JOIN SUCURSAL S	ON S.IdSucursal = CL.IdSucursal
			INNER JOIN EMPLEADO E	ON E.IdEmpleado = CL.IdEmpleado
			WHERE CL.FechaBaja IS NULL
			  AND (@IdSucursal IS NULL OR CL.IdSucursal = @IdSucursal)
			  AND (@IdEmpleado IS NULL OR CL.IdEmpleado = @IdEmpleado)
			  AND (@Busqueda IS NULL OR CL.Nombre LIKE '%' + @Busqueda + '%')
			  AND (@FechaDesde IS NULL OR CL.Fecha >= @FechaDesde)
			  AND (@FechaHasta IS NULL OR CL.Fecha <= @FechaHasta)
		) X
		ORDER BY X.Fecha DESC, X.HoraInicio
	END

	-- 5. RUTINAS ASIGNADAS (historial; grupo muscular y dificultad filtran por los ejercicios del detalle)
	IF @Accion = 5
	BEGIN
		SELECT
			R.IdRutina,
			R.FechaAsignacion,
			C.Folio,
			LTRIM(RTRIM(CONCAT(C.Nombre, ' ', C.PrimerApellido, ' ', ISNULL(C.SegundoApellido, '')))) AS Socio,
			LTRIM(RTRIM(CONCAT(E.Nombre, ' ', E.PrimerApellido))) AS Entrenador,
			R.Nombre		AS Rutina,
			R.Objetivo,
			COUNT(D.IdDetalleRutina)		AS TotalEjercicios,
			COUNT(DISTINCT D.DiaSemana)		AS TotalDias
		FROM RUTINA R
		INNER JOIN CLIENTE C			ON C.IdCliente = R.IdCliente
		INNER JOIN EMPLEADO E			ON E.IdEmpleado = R.IdEmpleado
		INNER JOIN DETALLE_RUTINA D		ON D.IdRutina = R.IdRutina
		INNER JOIN EJERCICIO EJ			ON EJ.IdEjercicio = D.IdEjercicio
		WHERE R.FechaBaja IS NULL
		  AND (@IdEmpleado IS NULL OR R.IdEmpleado = @IdEmpleado)
		  AND (@Objetivo IS NULL OR R.Objetivo = @Objetivo)
		  AND (@GrupoMuscular IS NULL OR EJ.GrupoMuscular = @GrupoMuscular)
		  AND (@NivelDificultad IS NULL OR EJ.NivelDificultad = @NivelDificultad)
		  AND (@Busqueda IS NULL
			   OR CAST(C.Folio AS NVARCHAR(10)) LIKE @Busqueda + '%'
			   OR CONCAT(C.Nombre, ' ', C.PrimerApellido, ' ', ISNULL(C.SegundoApellido, '')) LIKE '%' + @Busqueda + '%')
		GROUP BY R.IdRutina, R.FechaAsignacion, C.Folio, C.Nombre, C.PrimerApellido, C.SegundoApellido, E.Nombre, E.PrimerApellido, R.Nombre, R.Objetivo
		ORDER BY R.FechaAsignacion DESC, R.IdRutina DESC
	END

	-- 6. EJERCICIOS DE LAS RUTINAS (un renglón por ejercicio; C# los junta con foreach para el reporte de rutinas)
	IF @Accion = 6
	BEGIN
		SELECT D.IdRutina, EJ.Nombre
		FROM DETALLE_RUTINA D
		INNER JOIN EJERCICIO EJ	ON EJ.IdEjercicio = D.IdEjercicio
		INNER JOIN RUTINA R		ON R.IdRutina = D.IdRutina
		WHERE R.FechaBaja IS NULL
		ORDER BY D.IdRutina, D.IdDetalleRutina
	END
END
GO

CREATE OR ALTER PROCEDURE [dbo].[spUsuario]
	@Accion				INT				=	NULL,
	@IdUsuario			INT				=	NULL,
	@IdEstatus			INT				=	NULL,
	@IdRol				INT				=	NULL,
	@IdEmpleado			INT				=	NULL,	-- Empleado al que se liga la cuenta (relación 1:1)
	@Cuenta				NVARCHAR(50)	=	NULL,
	@Contrasenia		NVARCHAR(50)	=	NULL,
	@ContraseniaNueva	NVARCHAR(50)	=	NULL,	-- Solo para cambiar contraseña desde el perfil
	@UsuarioSesion		INT				=	NULL	-- Usuario que realiza la acción (auditoría)
AS
BEGIN
	SET NOCOUNT ON;

	-- VALIDAR INICIO DE SESIÓN (LOGIN)
	IF @Accion = 1
	BEGIN
		SELECT TOP 1
			U.IdUsuario,
			U.Cuenta,
			U.IdEstatus,
			ES.Descripcion		AS Estatus,
			U.IdRol,
			R.Descripcion		AS Rol,
			E.IdEmpleado,
			E.Nombre			AS Empleado,
			E.PrimerApellido,
			E.SegundoApellido,
			E.IdSucursal,
			E.IdEstatus			AS IdEstatusEmpleado	-- El login exige un empleado activo ligado
		FROM USUARIO U
		INNER JOIN ROL R		ON R.IdRol = U.IdRol
		INNER JOIN ESTATUS ES	ON ES.IdEstatus = U.IdEstatus
		LEFT JOIN EMPLEADO E	ON E.IdUsuario = U.IdUsuario	AND E.FechaBaja IS NULL	-- LEFT: si no hay empleado, C# rechaza el acceso con un mensaje claro
		WHERE U.Cuenta = @Cuenta
		  AND U.Contrasenia = @Contrasenia COLLATE Latin1_General_CS_AS	-- contraseña sensible a mayúsculas
		  AND U.FechaBaja IS NULL
	END

	-- OBTENER DATOS DE SESIÓN/PERFIL POR IDUSUARIO
	IF @Accion = 2
	BEGIN
		SELECT
			U.IdUsuario,
			U.Cuenta,
			U.IdEstatus,
			ES.Descripcion		AS EstatusDescripcion,
			U.IdRol,
			R.Descripcion		AS RolNombre,
			E.IdEmpleado,
			LTRIM(RTRIM(CONCAT(E.Nombre, ' ', E.PrimerApellido, ' ', ISNULL(E.SegundoApellido, '')))) AS NombreEmpleado,
			E.Correo,
			E.IdSucursal,
			S.Nombre			AS NombreSucursal
		FROM USUARIO U
		INNER JOIN ROL R		ON R.IdRol = U.IdRol
		INNER JOIN ESTATUS ES	ON ES.IdEstatus = U.IdEstatus
		LEFT JOIN EMPLEADO E	ON E.IdUsuario = U.IdUsuario
		LEFT JOIN SUCURSAL S	ON S.IdSucursal = E.IdSucursal
		WHERE U.IdUsuario = @IdUsuario
		  AND U.FechaBaja IS NULL
	END

	-- CONSULTAR USUARIOS (pantalla de gestión, filtros opcionales)
	IF @Accion = 3
	BEGIN
		SELECT
			U.IdUsuario,
			U.Cuenta,
			R.Descripcion		AS Rol,
			ES.Descripcion		AS Estatus,
			U.IdEstatus,
			LTRIM(RTRIM(CONCAT(E.Nombre, ' ', E.PrimerApellido, ' ', ISNULL(E.SegundoApellido, '')))) AS Empleado,
			U.FechaRegistro
		FROM USUARIO U
		INNER JOIN ROL R		ON R.IdRol = U.IdRol
		INNER JOIN ESTATUS ES	ON ES.IdEstatus = U.IdEstatus
		LEFT JOIN EMPLEADO E	ON E.IdUsuario = U.IdUsuario
		WHERE U.FechaBaja IS NULL
		  AND (@Cuenta IS NULL OR U.Cuenta LIKE '%' + @Cuenta + '%')
		  AND (@IdRol IS NULL OR U.IdRol = @IdRol)
		  AND (@IdEstatus IS NULL OR U.IdEstatus = @IdEstatus)
		ORDER BY U.Cuenta
	END

	-- LEER UN USUARIO PARA EDITARLO (no regresa la contraseña)
	IF @Accion = 4
	BEGIN
		SELECT
			U.IdUsuario,
			U.Cuenta,
			U.IdRol,
			U.IdEstatus,
			E.IdEmpleado
		FROM USUARIO U
		LEFT JOIN EMPLEADO E ON E.IdUsuario = U.IdUsuario
		WHERE U.IdUsuario = @IdUsuario
		  AND U.FechaBaja IS NULL
	END

	-- PUESTO Y ROL (alta y edición): el puesto Entrenador va con el rol Entrenador y viceversa.
	-- Al registrar se revisa el empleado elegido; al editar, el empleado ya ligado a la cuenta.
	IF @Accion IN (5, 6)
	BEGIN
		DECLARE @PuestoEntrenador BIT = CASE WHEN EXISTS (
			SELECT 1 FROM EMPLEADO E
			INNER JOIN PUESTO P ON P.IdPuesto = E.IdPuesto
			WHERE P.Nombre = 'Entrenador'
			  AND ((@Accion = 5 AND E.IdEmpleado = @IdEmpleado) OR (@Accion = 6 AND E.IdUsuario = @IdUsuario))
		) THEN 1 ELSE 0 END

		DECLARE @RolEntrenador BIT = CASE WHEN EXISTS (
			SELECT 1 FROM ROL WHERE IdRol = @IdRol AND Descripcion = 'Entrenador'
		) THEN 1 ELSE 0 END

		IF @PuestoEntrenador = 1 AND @RolEntrenador = 0
		BEGIN
			SELECT 'El empleado tiene el puesto Entrenador: su cuenta debe tener el rol Entrenador.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF @PuestoEntrenador = 0 AND @RolEntrenador = 1
		BEGIN
			SELECT 'El rol Entrenador solo se puede asignar a empleados con el puesto Entrenador.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END
	END

	-- REGISTRAR USUARIO Y LIGARLO A SU EMPLEADO
	IF @Accion = 5
	BEGIN
		-- Cuenta única (reemplaza la restricción UNIQUE que se quitó de la tabla)
		IF EXISTS (SELECT 1 FROM USUARIO WHERE Cuenta = @Cuenta AND FechaBaja IS NULL)
		BEGIN
			SELECT 'La cuenta "' + @Cuenta + '" ya está registrada.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		-- Relación 1:1: el empleado debe estar activo y sin cuenta
		IF NOT EXISTS (SELECT 1 FROM EMPLEADO
					   WHERE IdEmpleado = @IdEmpleado AND IdEstatus = 1 AND FechaBaja IS NULL AND IdUsuario IS NULL)
		BEGIN
			SELECT 'El empleado seleccionado no está activo o ya tiene una cuenta asignada.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		INSERT INTO USUARIO (IdEstatus, IdRol, Cuenta, Contrasenia, FechaRegistro, UsuarioRegistro)
		VALUES (@IdEstatus, @IdRol, @Cuenta, @Contrasenia, GETDATE(), @UsuarioSesion)

		UPDATE EMPLEADO
		SET IdUsuario			= SCOPE_IDENTITY(),
			FechaModificacion	= GETDATE(),
			UsuarioModificacion	= @UsuarioSesion
		WHERE IdEmpleado = @IdEmpleado

		SELECT 'Usuario registrado correctamente.' AS Mensaje, 'success' AS TipoMensaje
	END

	-- MODIFICAR USUARIO (si @Contrasenia viene vacía se conserva la actual; el empleado no cambia)
	IF @Accion = 6
	BEGIN
		IF EXISTS (SELECT 1 FROM USUARIO WHERE Cuenta = @Cuenta AND IdUsuario <> @IdUsuario AND FechaBaja IS NULL)
		BEGIN
			SELECT 'La cuenta "' + @Cuenta + '" ya está registrada por otro usuario.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		UPDATE USUARIO
		SET Cuenta				= @Cuenta,
			IdRol				= @IdRol,
			IdEstatus			= @IdEstatus,
			Contrasenia			= CASE WHEN NULLIF(@Contrasenia, '') IS NULL THEN Contrasenia ELSE @Contrasenia END,
			FechaModificacion	= GETDATE(),
			UsuarioModificacion	= @UsuarioSesion
		WHERE IdUsuario = @IdUsuario
		  AND FechaBaja IS NULL

		IF @@ROWCOUNT = 0
			SELECT 'No se encontró el usuario a modificar.' AS Mensaje, 'warning' AS TipoMensaje
		ELSE
			SELECT 'Usuario actualizado correctamente.' AS Mensaje, 'success' AS TipoMensaje
	END

	-- ELIMINAR USUARIO (baja lógica; el empleado queda libre para otra cuenta)
	IF @Accion = 7
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM USUARIO WHERE IdUsuario = @IdUsuario AND FechaBaja IS NULL)
		BEGIN
			SELECT 'No se encontró el usuario a eliminar.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		UPDATE USUARIO
		SET FechaBaja	= GETDATE(),
			UsuarioBaja	= @UsuarioSesion,
			IdEstatus	= 2		-- Inactivo
		WHERE IdUsuario = @IdUsuario

		UPDATE EMPLEADO
		SET IdUsuario			= NULL,
			FechaModificacion	= GETDATE(),
			UsuarioModificacion	= @UsuarioSesion
		WHERE IdUsuario = @IdUsuario

		SELECT 'Usuario eliminado correctamente.' AS Mensaje, 'success' AS TipoMensaje
	END

	-- ¿EXISTE LA CUENTA? (para validar en C# antes de registrar; @IdUsuario excluye al que se edita)
	IF @Accion = 10
	BEGIN
		SELECT COUNT(*) AS Total
		FROM USUARIO
		WHERE Cuenta = @Cuenta
		  AND FechaBaja IS NULL
		  AND IdUsuario <> ISNULL(@IdUsuario, 0)
	END

	-- EMPLEADOS DISPONIBLES PARA UNA CUENTA (activos y sin usuario; al editar incluye el suyo)
	IF @Accion = 11
	BEGIN
		SELECT
			E.IdEmpleado,
			LTRIM(RTRIM(CONCAT(E.Nombre, ' ', E.PrimerApellido, ' ', ISNULL(E.SegundoApellido, '')))) + ' (' + P.Nombre + ')' AS Empleado
		FROM EMPLEADO E
		INNER JOIN PUESTO P ON P.IdPuesto = E.IdPuesto
		WHERE E.FechaBaja IS NULL
		  AND E.IdEstatus = 1
		  AND (E.IdUsuario IS NULL OR E.IdUsuario = @IdUsuario)
		ORDER BY E.Nombre, E.PrimerApellido
	END

	-- CAMBIAR CONTRASEÑA DESDE EL PERFIL (valida la contraseña actual)
	IF @Accion = 12
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM USUARIO
					   WHERE IdUsuario = @IdUsuario AND FechaBaja IS NULL
					     AND Contrasenia = @Contrasenia COLLATE Latin1_General_CS_AS)
		BEGIN
			SELECT 'La contraseña actual no es correcta.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		UPDATE USUARIO
		SET Contrasenia			= @ContraseniaNueva,
			FechaModificacion	= GETDATE(),
			UsuarioModificacion	= @IdUsuario
		WHERE IdUsuario = @IdUsuario

		SELECT 'Contraseña actualizada correctamente.' AS Mensaje, 'success' AS TipoMensaje
	END

	-- PUESTO DEL EMPLEADO DE UNA CUENTA (por @IdEmpleado al registrar o por @IdUsuario al editar)
	IF @Accion = 13
	BEGIN
		SELECT TOP 1 P.Nombre AS Puesto
		FROM EMPLEADO E
		INNER JOIN PUESTO P ON P.IdPuesto = E.IdPuesto
		WHERE (@IdEmpleado IS NOT NULL AND E.IdEmpleado = @IdEmpleado)
		   OR (@IdEmpleado IS NULL AND E.IdUsuario = @IdUsuario)
	END
END

GO

-- =============================================================
-- CATALOGOS: todos los combos de catalogos del sistema en un solo SP.
-- Todas las acciones regresan la misma estructura: IdCatalogo y Descripcion
-- (los ejercicios traen ademas los datos que usa el diseñador de rutinas).
-- =============================================================
CREATE OR ALTER PROCEDURE [dbo].[spCatalogo]
	@Accion				INT				=	NULL
AS
BEGIN
	SET NOCOUNT ON;

	-- 1. ESTADOS DE LA REPUBLICA (sucursales)
	IF @Accion = 1
	BEGIN
		SELECT IdEstado AS IdCatalogo, Descripcion AS Descripcion
		FROM ESTADO
		ORDER BY Descripcion
	END

	-- 2. ROLES (usuarios)
	IF @Accion = 2
	BEGIN
		SELECT IdRol AS IdCatalogo, Descripcion AS Descripcion
		FROM ROL
		ORDER BY Descripcion
	END

	-- 3. PUESTOS (empleados y reporte de empleados)
	IF @Accion = 3
	BEGIN
		SELECT IdPuesto AS IdCatalogo, Nombre AS Descripcion
		FROM PUESTO
		ORDER BY Nombre
	END

	-- 4. ESPECIALIDADES (empleados y reporte de empleados)
	IF @Accion = 4
	BEGIN
		SELECT IdEspecialidad AS IdCatalogo, Nombre AS Descripcion
		FROM ESPECIALIDAD
		ORDER BY Nombre
	END

	-- 5. TIPOS DE MEMBRESIA (filtros de socios, suscripciones y reporte)
	IF @Accion = 5
	BEGIN
		SELECT IdTipoMembresia AS IdCatalogo, Nombre AS Descripcion
		FROM TIPO_MEMBRESIA
		WHERE FechaBaja IS NULL
		ORDER BY Precio
	END

	-- 6. TIPOS DE MEMBRESIA ACTIVOS PARA VENDER (el texto lleva precio y clases por semana)
	IF @Accion = 6
	BEGIN
		SELECT
			IdTipoMembresia AS IdCatalogo,
			Nombre + ' - $' + FORMAT(Precio, 'N2') + ' (' + CAST(MaxClasesSemana AS NVARCHAR(5)) + ' clases/sem)' AS Descripcion
		FROM TIPO_MEMBRESIA
		WHERE FechaBaja IS NULL AND IdEstatus = 1
		ORDER BY Precio
	END

	-- 7. ESTATUS DE SUSCRIPCION (Activo, Vencida, Cancelada)
	IF @Accion = 7
	BEGIN
		SELECT IdEstatus AS IdCatalogo, Descripcion AS Descripcion
		FROM ESTATUS
		WHERE IdEstatus IN (1, 3, 4)
		ORDER BY IdEstatus
	END

	-- 8. ESTATUS DE RESERVA (Cancelada, Confirmada, Asistencia registrada, Falta)
	IF @Accion = 8
	BEGIN
		SELECT IdEstatus AS IdCatalogo, Descripcion AS Descripcion
		FROM ESTATUS
		WHERE IdEstatus IN (4, 5, 6, 7)
		ORDER BY IdEstatus
	END

	-- 9. EJERCICIOS ACTIVOS (diseñador de rutinas; trae también grupo, nivel y contraindicación)
	IF @Accion = 9
	BEGIN
		SELECT IdEjercicio AS IdCatalogo, Nombre AS Descripcion, GrupoMuscular, NivelDificultad, ContraIndicacion
		FROM EJERCICIO
		WHERE FechaBaja IS NULL AND IdEstatus = 1
		ORDER BY GrupoMuscular, Nombre
	END
END
GO

-- =============================================================
-- SP de catalogos que ya no se usan: sus combos estan en spCatalogo
-- =============================================================
DROP PROCEDURE IF EXISTS [dbo].[spEjercicio]
DROP PROCEDURE IF EXISTS [dbo].[spMembresia]
DROP PROCEDURE IF EXISTS [dbo].[spPuesto]
DROP PROCEDURE IF EXISTS [dbo].[spEspecialidad]
GO

-- Funciones que ya no se usan: las listas con coma ahora se arman en C# con foreach
DROP FUNCTION IF EXISTS [dbo].[fnEspecialidadesEmpleado]
DROP FUNCTION IF EXISTS [dbo].[fnIdsEspecialidadesEmpleado]
DROP FUNCTION IF EXISTS [dbo].[fnEjerciciosRutina]
GO
