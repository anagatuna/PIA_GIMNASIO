-- =============================================================
-- CORRECCIONES (revision del PIA)
-- Correr este script sobre DBGIMNASIO despues del script completo.
-- Solo modifica stored procedures (CREATE OR ALTER); no borra datos.
-- =============================================================
USE [DBGIMNASIO]
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

	IF @Accion IN (1, 4)
	BEGIN
		IF ISNULL(@AforoMaximo, 0) <= 0 OR @AforoMaximo > 5000
		BEGIN
			SELECT N'El aforo máximo debe estar entre 1 y 5000 personas.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF @HoraCierre <= @HoraApertura
		BEGIN
			SELECT N'La hora de cierre debe ser posterior a la hora de apertura.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF EXISTS (SELECT 1 FROM SUCURSAL WHERE Nombre = @Nombre AND FechaBaja IS NULL AND IdSucursal <> ISNULL(@IdSucursal, 0))
		BEGIN
			SELECT N'Ya existe una sucursal llamada "' + @Nombre + N'".' AS Mensaje, 'warning' AS TipoMensaje
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
			SELECT N'El aforo no puede ser menor a los ' + CAST(@DentroAhora AS NVARCHAR(10)) + N' socios que están dentro de la sucursal.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF @AforoMaximo < @CupoMayor
		BEGIN
			SELECT N'El aforo no puede ser menor a ' + CAST(@CupoMayor AS NVARCHAR(10)) + N': hay clases programadas con ese cupo.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF EXISTS (SELECT 1 FROM SUCURSAL WHERE IdSucursal = @IdSucursal AND (HoraApertura <> @HoraApertura OR HoraCierre <> @HoraCierre))
		   AND EXISTS (SELECT 1 FROM CLASE WHERE IdSucursal = @IdSucursal AND FechaBaja IS NULL AND Fecha >= CAST(GETDATE() AS DATE))
		BEGIN
			SELECT N'No se puede modificar el horario: la sucursal tiene clases programadas.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END
	END

	IF @Accion = 1
	BEGIN
		INSERT INTO SUCURSAL (IdEstatus, IdEstado, Nombre, Telefono, AforoMaximo, HoraApertura, HoraCierre,
							  Colonia, Calle, NumeroInt, CodigoPostal, Municipio, FechaRegistro, UsuarioRegistro)
		VALUES (@IdEstatus, @IdEstado, @Nombre, @Telefono, @AforoMaximo, @HoraApertura, @HoraCierre,
				@Colonia, @Calle, @NumeroInt, @CodigoPostal, @Municipio, GETDATE(), @UsuarioSesion)

		SELECT N'Sucursal registrada correctamente.' AS Mensaje, 'success' AS TipoMensaje
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
			S.Calle + N' ' + S.NumeroInt + N', ' + S.Colonia + N', ' + S.Municipio + N', ' + ED.Descripcion + N' C.P. ' + S.CodigoPostal AS Direccion,
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
			SELECT N'No se encontró la sucursal a modificar.' AS Mensaje, 'warning' AS TipoMensaje
		ELSE
			SELECT N'Sucursal actualizada correctamente.' AS Mensaje, 'success' AS TipoMensaje
	END

	-- ELIMINAR (baja lógica; no si tiene personal o socios activos)
	IF @Accion = 5
	BEGIN
		IF EXISTS (SELECT 1 FROM EMPLEADO WHERE IdSucursal = @IdSucursal AND FechaBaja IS NULL)
		BEGIN
			SELECT N'No se puede eliminar: la sucursal tiene empleados activos.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF EXISTS (SELECT 1 FROM CLIENTE WHERE IdSucursal = @IdSucursal AND FechaBaja IS NULL)
		BEGIN
			SELECT N'No se puede eliminar: la sucursal tiene socios registrados.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		UPDATE SUCURSAL
		SET FechaBaja = GETDATE(), UsuarioBaja = @UsuarioSesion, IdEstatus = 2
		WHERE IdSucursal = @IdSucursal AND FechaBaja IS NULL

		IF @@ROWCOUNT = 0
			SELECT N'No se encontró la sucursal a eliminar.' AS Mensaje, 'warning' AS TipoMensaje
		ELSE
			SELECT N'Sucursal eliminada correctamente.' AS Mensaje, 'success' AS TipoMensaje
	END

	-- COMBO DE SUCURSALES ACTIVAS (lo usan empleados, socios, clases y control de acceso)
	IF @Accion = 6
	BEGIN
		SELECT IdSucursal, Nombre, AforoMaximo, HoraApertura, HoraCierre
		FROM SUCURSAL
		WHERE FechaBaja IS NULL AND IdEstatus = 1
		ORDER BY Nombre
	END

	-- COMBO DE ESTADOS DE LA REPÚBLICA
	IF @Accion = 7
	BEGIN
		SELECT IdEstado, Descripcion FROM ESTADO ORDER BY Descripcion
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

		DECLARE @Resultado NVARCHAR(10) = N'Denegado', @Mensaje NVARCHAR(200)

		BEGIN TRY
			BEGIN TRANSACTION

			-- Bloquea la sucursal para que dos entradas simultáneas no rebasen el aforo
			DECLARE @AforoMaximo INT, @Ocupacion INT, @Abre TIME, @Cierra TIME
			DECLARE @HoraActual TIME = CAST(GETDATE() AS TIME)
			SELECT @AforoMaximo = AforoMaximo, @Abre = HoraApertura, @Cierra = HoraCierre
			FROM SUCURSAL WITH (UPDLOCK, HOLDLOCK)
			WHERE IdSucursal = @IdSucursal AND IdEstatus = 1 AND FechaBaja IS NULL

			SELECT @Ocupacion = COUNT(*) FROM CONTROL_ACCESO
			WHERE IdSucursal = @IdSucursal AND FechaSalida IS NULL AND CAST(FechaEntrada AS DATE) = @Hoy

			IF @AforoMaximo IS NULL
				SET @Mensaje = N'La sucursal no está activa.'
			ELSE IF @HoraActual < @Abre OR @HoraActual > @Cierra
				SET @Mensaje = N'La sucursal está cerrada. Horario: ' + LEFT(CAST(@Abre AS NVARCHAR(8)), 5) + N' - ' + LEFT(CAST(@Cierra AS NVARCHAR(8)), 5) + N'.'
			ELSE IF @IdCliente IS NULL
				SET @Mensaje = N'No existe un socio con el folio ' + ISNULL(CAST(@Folio AS NVARCHAR(10)), N'capturado') + N'.'
			ELSE IF @EstatusCliente <> 1
				SET @Mensaje = N'El socio está inactivo.'
			ELSE IF @Membresia IS NULL
				SET @Mensaje = N'Membresía vencida o sin suscripción activa.'
			ELSE IF EXISTS (SELECT 1 FROM CONTROL_ACCESO WHERE IdCliente = @IdCliente AND FechaSalida IS NULL AND CAST(FechaEntrada AS DATE) = @Hoy)
				SET @Mensaje = N'El socio ya tiene una entrada registrada sin salida.'
			ELSE IF @Ocupacion >= @AforoMaximo
				SET @Mensaje = N'Aforo máximo alcanzado (' + CAST(@Ocupacion AS NVARCHAR(10)) + N'/' + CAST(@AforoMaximo AS NVARCHAR(10)) + N').'
			ELSE
			BEGIN
				INSERT INTO CONTROL_ACCESO (IdSucursal, IdCliente, FechaEntrada) VALUES (@IdSucursal, @IdCliente, GETDATE())
				SET @Ocupacion = @Ocupacion + 1
				SET @Resultado = N'Permitido'
				SET @Mensaje = N'Acceso permitido. ¡Bienvenido!'
			END

			COMMIT TRANSACTION
		END TRY
		BEGIN CATCH
			IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION
			SET @Mensaje = N'No se pudo registrar la entrada: ' + ERROR_MESSAGE()
		END CATCH

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
			SELECT N'Denegado' AS Resultado, N'El folio ' + ISNULL(CAST(@Folio AS NVARCHAR(10)), N'capturado') + N' no tiene una entrada abierta en esta sucursal.' AS Mensaje,
				   @Folio AS Folio, NULL AS Socio, NULL AS Membresia, NULL AS FechaVencimiento, NULL AS Minutos,
				   (SELECT COUNT(*) FROM CONTROL_ACCESO WHERE IdSucursal = @IdSucursal AND FechaSalida IS NULL AND CAST(FechaEntrada AS DATE) = @Hoy) AS Ocupacion,
				   (SELECT AforoMaximo FROM SUCURSAL WHERE IdSucursal = @IdSucursal) AS AforoMaximo
			RETURN
		END

		UPDATE CONTROL_ACCESO SET FechaSalida = @Salida WHERE IdControlAcceso = @IdControl

		DECLARE @Minutos INT = DATEDIFF(MINUTE, @Entrada, @Salida)

		SELECT N'Salida' AS Resultado,
			   N'Salida registrada. Permanencia: ' + CAST(@Minutos AS NVARCHAR(10)) + N' minutos.' AS Mensaje,
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

	IF @Accion IN (1, 4)
	BEGIN
		IF EXISTS (SELECT 1 FROM CLIENTE WHERE Correo = @Correo AND FechaBaja IS NULL AND IdCliente <> ISNULL(@IdCliente, 0))
		BEGIN
			SELECT N'El correo ' + @Correo + N' ya pertenece a otro socio.' AS Mensaje, 'warning' AS TipoMensaje, 0 AS Folio
			RETURN
		END

		IF NOT EXISTS (SELECT 1 FROM SUCURSAL WHERE IdSucursal = @IdSucursal AND IdEstatus = 1 AND FechaBaja IS NULL)
		BEGIN
			SELECT N'La sucursal seleccionada no está activa.' AS Mensaje, 'warning' AS TipoMensaje, 0 AS Folio
			RETURN
		END
	END

	-- REGISTRAR SOCIO + EXPEDIENTE MÉDICO. El folio se genera aquí: Año actual + consecutivo de 4 dígitos (ej. 20260001)
	IF @Accion = 1
	BEGIN
		IF NULLIF(LTRIM(@TipoSangre), '') IS NULL
		BEGIN
			SELECT N'El expediente médico es obligatorio: indica al menos el tipo de sangre.' AS Mensaje, 'warning' AS TipoMensaje, 0 AS Folio
			RETURN
		END

		BEGIN TRY
			BEGIN TRANSACTION

			DECLARE @Anio INT = YEAR(GETDATE())
			DECLARE @NuevoFolio INT

			-- UPDLOCK + HOLDLOCK: dos recepcionistas registrando al mismo tiempo no obtienen el mismo folio
			SELECT @NuevoFolio = ISNULL(MAX(Folio), @Anio * 10000) + 1
			FROM CLIENTE WITH (UPDLOCK, HOLDLOCK)
			WHERE Folio BETWEEN @Anio * 10000 AND @Anio * 10000 + 9999

			IF @NuevoFolio > @Anio * 10000 + 9999
			BEGIN
				ROLLBACK TRANSACTION
				SELECT N'Se alcanzó el máximo de folios del año.' AS Mensaje, 'danger' AS TipoMensaje, 0 AS Folio
				RETURN
			END

			INSERT INTO CLIENTE (IdSucursal, IdEstatus, Folio, Nombre, PrimerApellido, SegundoApellido, FechaNacimiento,
								 Correo, Telefono, FechaInscripcion, FechaRegistro, UsuarioRegistro)
			VALUES (@IdSucursal, @IdEstatus, @NuevoFolio, @Nombre, @PrimerApellido, @SegundoApellido, @FechaNacimiento,
					@Correo, @Telefono, GETDATE(), GETDATE(), @UsuarioSesion)

			DECLARE @IdNuevo INT = SCOPE_IDENTITY()

			INSERT INTO EXPEDIENTE_MEDICO (IdCliente, TipoSangre, PadecimientosCronicos, LesionesPrevias, Alergias, CondicionRiesgo)
			VALUES (@IdNuevo, @TipoSangre, @PadecimientosCronicos, @LesionesPrevias, @Alergias, @CondicionRiesgo)

			COMMIT TRANSACTION
			-- IdCliente: la página lo usa para mandar al socio nuevo a Suscripción (membresía obligatoria)
			SELECT N'Socio registrado correctamente con el folio ' + CAST(@NuevoFolio AS NVARCHAR(10)) + N'.' AS Mensaje,
				   'success' AS TipoMensaje, @NuevoFolio AS Folio, @IdNuevo AS IdCliente
		END TRY
		BEGIN CATCH
			IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION
			SELECT N'No se pudo registrar el socio: ' + ERROR_MESSAGE() AS Mensaje, 'danger' AS TipoMensaje, 0 AS Folio
		END CATCH
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
			SELECT N'No se encontró el socio a modificar.' AS Mensaje, 'warning' AS TipoMensaje, 0 AS Folio
		ELSE
			SELECT N'Socio actualizado correctamente.' AS Mensaje, 'success' AS TipoMensaje, 0 AS Folio
	END

	-- ELIMINAR (baja lógica; no si tiene suscripción activa)
	IF @Accion = 5
	BEGIN
		IF EXISTS (SELECT 1 FROM SUSCRIPCION WHERE IdCliente = @IdCliente AND IdEstatus = 1 AND FechaBaja IS NULL)
		BEGIN
			SELECT N'No se puede eliminar: el socio tiene una suscripción activa. Cancélala primero.' AS Mensaje, 'warning' AS TipoMensaje, 0 AS Folio
			RETURN
		END

		UPDATE CLIENTE
		SET FechaBaja = GETDATE(), UsuarioBaja = @UsuarioSesion, IdEstatus = 2
		WHERE IdCliente = @IdCliente AND FechaBaja IS NULL

		IF @@ROWCOUNT = 0
			SELECT N'No se encontró el socio a eliminar.' AS Mensaje, 'warning' AS TipoMensaje, 0 AS Folio
		ELSE
			SELECT N'Socio eliminado correctamente.' AS Mensaje, 'success' AS TipoMensaje, 0 AS Folio
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

	EXEC spVencerSuscripciones

	-- VENDER / RENOVAR: vigencia automática de 1 mes y máximo 1 suscripción activa por socio
	IF @Accion = 1
	BEGIN
		IF NOT EXISTS (SELECT 1 FROM CLIENTE WHERE IdCliente = @IdCliente AND IdEstatus = 1 AND FechaBaja IS NULL)
		BEGIN
			SELECT N'El socio seleccionado no está activo.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF NOT EXISTS (SELECT 1 FROM TIPO_MEMBRESIA WHERE IdTipoMembresia = @IdTipoMembresia AND IdEstatus = 1 AND FechaBaja IS NULL)
		BEGIN
			SELECT N'La membresía seleccionada no está activa.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF @FechaInicio IS NULL OR @FechaInicio < CAST(GETDATE() AS DATE)
		BEGIN
			SELECT N'La fecha de inicio no puede ser anterior a hoy.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF @FechaInicio > DATEADD(MONTH, 1, CAST(GETDATE() AS DATE))
		BEGIN
			SELECT N'La fecha de inicio no puede ser mayor a un mes a partir de hoy.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF @FechaInicio < (SELECT CAST(FechaInscripcion AS DATE) FROM CLIENTE WHERE IdCliente = @IdCliente)
		BEGIN
			SELECT N'La fecha de inicio no puede ser anterior a la inscripción del socio.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		BEGIN TRY
			BEGIN TRANSACTION

			-- Bloquea al socio: dos ventas simultáneas no pueden dejarle 2 suscripciones activas
			-- (reemplaza al índice único filtrado que se quitó de la tabla)
			DECLARE @Bloqueo INT
			SELECT @Bloqueo = IdCliente FROM CLIENTE WITH (UPDLOCK, HOLDLOCK) WHERE IdCliente = @IdCliente

			DECLARE @Vence DATE = (SELECT TOP 1 FechaVencimiento FROM SUSCRIPCION
								   WHERE IdCliente = @IdCliente AND IdEstatus = 1 AND FechaBaja IS NULL)
			IF @Vence IS NOT NULL
			BEGIN
				COMMIT TRANSACTION		-- No se escribió nada; solo libera el bloqueo
				SELECT N'El socio ya tiene una suscripción activa (vence el ' + CONVERT(NVARCHAR(10), @Vence, 103)
					   + N'). Para agregar otro mes usa el botón Renovar.' AS Mensaje, 'warning' AS TipoMensaje
				RETURN
			END

			INSERT INTO SUSCRIPCION (IdEstatus, IdCliente, IdTipoMembresia, FechaInicio, FechaVencimiento, FechaRegistro, UsuarioRegistro)
			VALUES (1, @IdCliente, @IdTipoMembresia, @FechaInicio, DATEADD(DAY, -1, DATEADD(MONTH, 1, @FechaInicio)), GETDATE(), @UsuarioSesion)

			COMMIT TRANSACTION
			SELECT N'Suscripción registrada. Vigente del ' + CONVERT(NVARCHAR(10), @FechaInicio, 103) + N' al '
				   + CONVERT(NVARCHAR(10), DATEADD(DAY, -1, DATEADD(MONTH, 1, @FechaInicio)), 103) + N'.' AS Mensaje, 'success' AS TipoMensaje
		END TRY
		BEGIN CATCH
			IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION
			SELECT N'No se pudo registrar la suscripción: ' + ERROR_MESSAGE() AS Mensaje, 'danger' AS TipoMensaje
		END CATCH
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
			SELECT N'Solo se pueden cancelar suscripciones activas.' AS Mensaje, 'warning' AS TipoMensaje
		ELSE
			SELECT N'Suscripción cancelada correctamente.' AS Mensaje, 'success' AS TipoMensaje
	END

	-- COMBO DE ESTATUS DE SUSCRIPCIÓN (Activo, Vencida, Cancelada)
	IF @Accion = 5
	BEGIN
		SELECT IdEstatus, Descripcion FROM ESTATUS WHERE IdEstatus IN (1, 3, 4) ORDER BY IdEstatus
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
			SELECT N'No se encontró la suscripción.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF NOT EXISTS (SELECT 1 FROM CLIENTE WHERE IdCliente = @Cliente AND IdEstatus = 1 AND FechaBaja IS NULL)
		BEGIN
			SELECT N'El socio no está activo.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF NOT EXISTS (SELECT 1 FROM TIPO_MEMBRESIA WHERE IdTipoMembresia = @Tipo AND IdEstatus = 1 AND FechaBaja IS NULL)
		BEGIN
			SELECT N'La membresía de esta suscripción ya no está activa; véndele una nueva.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF EXISTS (SELECT 1 FROM SUSCRIPCION WHERE IdCliente = @Cliente AND FechaBaja IS NULL AND IdSuscripcion > @IdSuscripcion)
		BEGIN
			SELECT N'Solo se puede renovar la última suscripción del socio.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF @Estatus = 1 AND @Inicio > @Hoy
		BEGIN
			SELECT N'El socio ya tiene una renovación programada (inicia el ' + CONVERT(NVARCHAR(10), @Inicio, 103) + N').' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		BEGIN TRY
			BEGIN TRANSACTION

			-- Reanudar la cancelada que todavía tiene días
			IF @Estatus = 4 AND @Fin >= @Hoy
			BEGIN
				UPDATE SUSCRIPCION
				SET IdEstatus = 1, FechaModificacion = GETDATE(), UsuarioModificacion = @UsuarioSesion
				WHERE IdSuscripcion = @IdSuscripcion

				COMMIT TRANSACTION
				SELECT N'Membresía reanudada. Vigente hasta el ' + CONVERT(NVARCHAR(10), @Fin, 103) + N'.' AS Mensaje, 'success' AS TipoMensaje
				RETURN
			END

			-- Nuevo mes: encadenado si sigue activa, o desde hoy
			DECLARE @NuevoInicio DATE = @Hoy
			IF @Estatus = 1
				SET @NuevoInicio = DATEADD(DAY, 1, @Fin)
			DECLARE @NuevoFin DATE = DATEADD(DAY, -1, DATEADD(MONTH, 1, @NuevoInicio))

			INSERT INTO SUSCRIPCION (IdEstatus, IdCliente, IdTipoMembresia, FechaInicio, FechaVencimiento, FechaRegistro, UsuarioRegistro)
			VALUES (1, @Cliente, @Tipo, @NuevoInicio, @NuevoFin, GETDATE(), @UsuarioSesion)

			COMMIT TRANSACTION
			SELECT N'Suscripción renovada. Vigente del ' + CONVERT(NVARCHAR(10), @NuevoInicio, 103) + N' al '
				   + CONVERT(NVARCHAR(10), @NuevoFin, 103) + N'.' AS Mensaje, 'success' AS TipoMensaje
		END TRY
		BEGIN CATCH
			IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION
			SELECT N'No se pudo renovar la suscripción: ' + ERROR_MESSAGE() AS Mensaje, 'danger' AS TipoMensaje
		END CATCH
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

	-- CAPTURAR RESERVA: cupo del salón + límite semanal del plan (Bronce 0, Plata 3, Oro 5)
	IF @Accion = 1
	BEGIN
		EXEC spVencerSuscripciones

		BEGIN TRY
			BEGIN TRANSACTION

			-- Bloquea la clase: dos reservas simultáneas no pueden pasarse del cupo
			DECLARE @FechaClase DATE, @HoraClase TIME, @Cupo INT
			SELECT @FechaClase = Fecha, @HoraClase = HoraInicio, @Cupo = CupoMaximo
			FROM CLASE WITH (UPDLOCK, HOLDLOCK)
			WHERE IdClase = @IdClase AND IdEstatus = 1 AND FechaBaja IS NULL

			DECLARE @Error NVARCHAR(200) = NULL

			IF @FechaClase IS NULL
				SET @Error = N'La clase seleccionada no existe o fue cancelada.'
			ELSE IF CAST(@FechaClase AS DATETIME) + CAST(@HoraClase AS DATETIME) <= GETDATE()
				SET @Error = N'La clase ya comenzó; no se pueden capturar reservas.'
			ELSE IF NOT EXISTS (SELECT 1 FROM CLIENTE WHERE IdCliente = @IdCliente AND IdEstatus = 1 AND FechaBaja IS NULL)
				SET @Error = N'El socio seleccionado no está activo.'

			-- Plan vigente el día de la clase
			DECLARE @Plan NVARCHAR(50), @MaxClases INT
			IF @Error IS NULL
			BEGIN
				SELECT TOP 1 @Plan = TM.Nombre, @MaxClases = TM.MaxClasesSemana
				FROM SUSCRIPCION SU
				INNER JOIN TIPO_MEMBRESIA TM ON TM.IdTipoMembresia = SU.IdTipoMembresia
				WHERE SU.IdCliente = @IdCliente AND SU.IdEstatus = 1 AND SU.FechaBaja IS NULL
				  AND @FechaClase BETWEEN SU.FechaInicio AND SU.FechaVencimiento

				IF @Plan IS NULL
					SET @Error = N'El socio no tiene una membresía vigente para la fecha de la clase.'
				ELSE IF @MaxClases = 0
					SET @Error = N'La membresía ' + @Plan + N' no incluye clases grupales.'
			END

			-- Límite semanal (semana de lunes a domingo de la clase)
			DECLARE @Lunes DATE = DATEADD(DAY, -(DATEDIFF(DAY, '19000101', @FechaClase) % 7), @FechaClase)
			DECLARE @Usadas INT
			IF @Error IS NULL
			BEGIN
				IF EXISTS (SELECT 1 FROM RESERVA WHERE IdCliente = @IdCliente AND IdClase = @IdClase AND IdEstatus IN (5, 6, 7) AND FechaBaja IS NULL)
					SET @Error = N'El socio ya tiene una reserva en esta clase.'
				ELSE
				BEGIN
					SELECT @Usadas = COUNT(*)
					FROM RESERVA R
					INNER JOIN CLASE CL ON CL.IdClase = R.IdClase
					WHERE R.IdCliente = @IdCliente AND R.IdEstatus IN (5, 6, 7) AND R.FechaBaja IS NULL
					  AND CL.Fecha BETWEEN @Lunes AND DATEADD(DAY, 6, @Lunes)

					IF @Usadas >= @MaxClases
						SET @Error = N'Límite semanal alcanzado: la membresía ' + @Plan + N' permite ' + CAST(@MaxClases AS NVARCHAR(5))
									 + N' clases por semana y ya tiene ' + CAST(@Usadas AS NVARCHAR(5)) + N'.'
				END
			END

			-- Cupo del salón
			IF @Error IS NULL AND (SELECT COUNT(*) FROM RESERVA WHERE IdClase = @IdClase AND IdEstatus IN (5, 6, 7) AND FechaBaja IS NULL) >= @Cupo
				SET @Error = N'La clase ya no tiene lugares disponibles (cupo de ' + CAST(@Cupo AS NVARCHAR(5)) + N').'

			IF @Error IS NOT NULL
			BEGIN
				COMMIT TRANSACTION		-- No se escribió nada; solo libera el bloqueo de la clase
				SELECT @Error AS Mensaje, 'warning' AS TipoMensaje
				RETURN
			END

			INSERT INTO RESERVA (FechaSolicitud, IdCliente, IdEmpleado, IdClase, IdEstatus, FechaRegistro, UsuarioRegistro)
			VALUES (GETDATE(), @IdCliente, @IdEmpleado, @IdClase, 5, GETDATE(), @UsuarioSesion)

			COMMIT TRANSACTION
			SELECT N'Reserva confirmada (' + CAST(@Usadas + 1 AS NVARCHAR(5)) + N' de ' + CAST(@MaxClases AS NVARCHAR(5))
				   + N' clases de la semana, plan ' + @Plan + N').' AS Mensaje, 'success' AS TipoMensaje
		END TRY
		BEGIN CATCH
			IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION
			SELECT N'No se pudo registrar la reserva: ' + ERROR_MESSAGE() AS Mensaje, 'danger' AS TipoMensaje
		END CATCH
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
			SELECT N'Solo se pueden cancelar reservas confirmadas de clases que no han comenzado.' AS Mensaje, 'warning' AS TipoMensaje
		ELSE
			SELECT N'Reserva cancelada correctamente.' AS Mensaje, 'success' AS TipoMensaje
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
			CL.Nombre + N' · ' + CONVERT(NVARCHAR(5), CL.Fecha, 103) + N' ' + LEFT(CAST(CL.HoraInicio AS NVARCHAR(8)), 5)
				+ N' · ' + S.Nombre + N' / ' + CL.Salon
				+ N' (' + CAST(CL.CupoMaximo - X.Reservadas AS NVARCHAR(5)) + N' lugares)' AS Clase
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

	-- COMBO DE ESTATUS DE RESERVA (Cancelada, Confirmada, Asistencia registrada, Falta)
	IF @Accion = 6
	BEGIN
		SELECT IdEstatus, Descripcion FROM ESTATUS WHERE IdEstatus IN (4, 5, 6, 7) ORDER BY IdEstatus
	END
END
GO

CREATE OR ALTER PROCEDURE [dbo].[spPaseLista]
	@Accion				INT				=	NULL,
	@IdClase			INT				=	NULL,
	@IdEmpleado			INT				=	NULL,	-- Entrenador (NULL = todas las clases, para el Administrador)
	@Fecha				DATE			=	NULL,
	@Detalle			NVARCHAR(MAX)	=	NULL,	-- JSON: [{"IdReserva":1,"IdEstatus":6}, ...]
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

	-- GUARDAR ASISTENCIA (Confirmado 5 / Asistió 6 / Falta 7)
	IF @Accion = 3
	BEGIN
		DECLARE @Inicio DATETIME, @Entrenador INT
		SELECT @Inicio = CAST(Fecha AS DATETIME) + CAST(HoraInicio AS DATETIME), @Entrenador = IdEmpleado
		FROM CLASE WHERE IdClase = @IdClase AND FechaBaja IS NULL

		IF @Inicio IS NULL
		BEGIN
			SELECT N'No se encontró la clase.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF @IdEmpleado IS NOT NULL AND @Entrenador <> @IdEmpleado
		BEGIN
			SELECT N'Solo puedes pasar lista en tus propias clases.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		IF @Inicio > GETDATE()
		BEGIN
			SELECT N'La clase todavía no comienza; el pase de lista se habilita a la hora de inicio.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		DECLARE @D TABLE (IdReserva INT, IdEstatus INT)
		IF ISJSON(@Detalle) = 1
			INSERT INTO @D SELECT IdReserva, IdEstatus FROM OPENJSON(@Detalle) WITH (IdReserva INT, IdEstatus INT)

		IF EXISTS (SELECT 1 FROM @D WHERE IdEstatus NOT IN (5, 6, 7) OR IdEstatus IS NULL)
		BEGIN
			SELECT N'Hay participantes con un estatus de asistencia no válido.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		-- Solo puede marcarse "Asistió" si el socio registró su entrada en la sucursal de la clase ese día
		DECLARE @SinEntrada NVARCHAR(500)
		SELECT @SinEntrada = ISNULL(@SinEntrada + N', ', N'') + CAST(C.Folio AS NVARCHAR(10))
		FROM @D D
		INNER JOIN RESERVA R	ON R.IdReserva = D.IdReserva AND R.IdClase = @IdClase
		INNER JOIN CLIENTE C	ON C.IdCliente = R.IdCliente
		INNER JOIN CLASE CL		ON CL.IdClase = R.IdClase
		WHERE D.IdEstatus = 6
		  AND NOT EXISTS (SELECT 1 FROM CONTROL_ACCESO CA
						  WHERE CA.IdCliente = R.IdCliente AND CA.IdSucursal = CL.IdSucursal
							AND CAST(CA.FechaEntrada AS DATE) = CL.Fecha)

		IF @SinEntrada IS NOT NULL
		BEGIN
			SELECT N'No se puede marcar asistencia sin entrada registrada en la sucursal ese día. Folios: ' + @SinEntrada + N'.' AS Mensaje, 'warning' AS TipoMensaje
			RETURN
		END

		UPDATE R
		SET R.IdEstatus = D.IdEstatus, R.FechaModificacion = GETDATE(), R.UsuarioModificacion = @UsuarioSesion
		FROM RESERVA R
		INNER JOIN @D D ON D.IdReserva = R.IdReserva
		WHERE R.IdClase = @IdClase AND R.FechaBaja IS NULL AND R.IdEstatus IN (5, 6, 7)

		SELECT N'Pase de lista guardado (' + CAST(@@ROWCOUNT AS NVARCHAR(5)) + N' participantes).' AS Mensaje, 'success' AS TipoMensaje
	END
END
GO
