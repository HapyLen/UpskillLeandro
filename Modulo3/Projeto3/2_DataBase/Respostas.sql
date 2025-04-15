
-- /////////////////////
-- 			1
-- /////////////////////

-- 1. Consultar o numero total de obras existentes/

CREATE PROCEDURE sp_total_obras
AS
BEGIN
	SELECT SUM(quantidade) AS TotalNumeroDeObras
	FROM [NucleoObra]
END
GO


-- /////////////////////
-- 			2
-- /////////////////////

-- 2. Consultar o numero de obras existentes por tipo de assunto

CREATE PROCEDURE sp_total_obras_por_genero
AS
BEGIN
	SELECT dbo.[Genero].nome_genero, SUM(dbo.[NucleoObra].quantidade) AS total_quantidade
	FROM dbo.[NucleoObra] 
		INNER JOIN dbo.[Obra] ON dbo.[NucleoObra].pk_obra = dbo.[Obra].pk_obra 
		INNER JOIN dbo.[GeneroObra] ON dbo.[Obra].pk_obra = dbo.[GeneroObra].pk_obra 
		INNER JOIN dbo.[Genero] ON dbo.[GeneroObra].pk_genero = dbo.[Genero].pk_genero
	GROUP BY dbo.[Genero].nome_genero
END
GO


-- /////////////////////
-- 			3
-- /////////////////////

-- 3. Consultar a lista das dez obras mais requisitadas num intervalo de tempo 
-- por ordem descrescente
-- //historico recebe automaticamente obras ou apenas as fechadas?
-- 

CREATE PROCEDURE sp_top_requested_by_time
(
	@start_date DATE = NULL,
	@end_date DATE = NULL
)
AS
	BEGIN
	SELECT TOP 10 dbo.[Obra].nome_obra, COUNT(dbo.[Requisicao].pk_obra) AS TimesRequested
	FROM dbo.[Obra] 
		INNER JOIN dbo.[Requisicao] ON dbo.[Obra].pk_obra = dbo.[Requisicao].pk_obra
	WHERE (dbo.[Requisicao].data_levantamento BETWEEN @start_date AND @end_date) --//TODO dinamic dates in case of passing null // can make and function
	GROUP BY dbo.[Requisicao].pk_obra, dbo.[Obra].nome_obra
	ORDER BY COUNT(dbo.[Obra].pk_obra) DESC
END
GO

-- /////////////////////
--			 4
-- /////////////////////

-- 4. Consultar a lista dos nucleos, por ordem decrescente de requisições num 
-- intervalo de tempo

CREATE PROCEDURE sp_requisicoes_by_nucleo
(
	@start_date DATE,
	@end_date DATE
)
AS
BEGIN
	SELECT dbo.[Nucleo].nome_nucleo, COUNT(dbo.[Requisicao].pk_obra) AS total_requisicoes
	FROM dbo.[Requisicao] 
		INNER JOIN dbo.[Nucleo] ON dbo.[Requisicao].pk_nucleo = dbo.[Nucleo].pk_nucleo
		INNER JOIN dbo.[Obra] ON dbo.[Requisicao].pk_obra = dbo.[Obra].pk_obra
	WHERE dbo.[Requisicao].data_levantamento BETWEEN @start_date AND @end_date
	GROUP BY dbo.[Nucleo].pk_nucleo, dbo.[Nucleo].nome_nucleo
	ORDER BY total_requisicoes DESC
END
GO


-- /////////////////////
-- 			5
-- /////////////////////

-- 5. Acrescentar novas obras

-- Se isbn procura a pk deste
CREATE PROCEDURE sp_insert_obra
(
	@pk_nucleo INT,
	@nome_obra NVARCHAR(50), --set values to default NULL
	@isbn NVARCHAR(50),
	@autor NVARCHAR(50),
	@editora NVARCHAR(50),
	@ano INT,
	@image_path NVARCHAR(50) = NULL,
	@quantidade INT = 0
)
AS
BEGIN
	DECLARE @initial_trancount INT = @@TRANCOUNT
	IF @initial_trancount = 0
		BEGIN TRANSACTION;
	BEGIN TRY
		--CHECKS:
		-- check if obra already exists
		IF EXISTS (SELECT * FROM dbo.[Obra] WHERE isbn = @isbn)
			THROW 50000, 'Error: obra already exists', 1;
		--check if nucleo exists
		IF @pk_nucleo IS NOT NULL AND NOT EXISTS (SELECT * FROM dbo.[Nucleo] WHERE pk_nucleo = @pk_nucleo)
			THROW 50000, 'Error: nucleo not found', 1;
		--//if nucleo is null, use central
		IF @pk_nucleo IS NULL
			SET @pk_nucleo = dbo.fn_get_central_nucleo()
		--ACTUALL INSERT
		--variable to store the new pk_obra
		DECLARE @new_pk_obra INT
		-- stat is defaulted to 'active'
		INSERT INTO dbo.[Obra] (nome_obra, isbn, editora, ano)
		VALUES (@nome_obra, @isbn, @editora, @ano)
		SET @new_pk_obra = SCOPE_IDENTITY()
		-- insert obra into nucleo relationship
		INSERT INTO dbo.[NucleoObra] (pk_nucleo, pk_obra, quantidade) --quantidade default 0
		VALUES (@pk_nucleo, @new_pk_obra, @quantidade)
		-- update image, if image path is not null
		EXEC dbo.sp_update_image @new_pk_obra, @image_path, @isbn
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			COMMIT TRANSACTION;
	END TRY
	BEGIN CATCH
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			ROLLBACK TRANSACTION;
		-- Re-THROW caught error
		THROW;
	END CATCH
END
GO

CREATE PROCEDURE sp_update_image
(
	@pk_obra INT,
	@image_path NVARCHAR(50),
	@isbn NVARCHAR(50)
)
AS
BEGIN
	DECLARE @initial_trancount INT = @@TRANCOUNT
	IF @initial_trancount = 0
		BEGIN TRANSACTION;
	BEGIN TRY
		-- declare variable to temp values
		DECLARE @new_image_pk INT
		DECLARE @sql NVARCHAR(MAX)
		IF (@image_path IS NOT NULL)
		BEGIN
			-- sql query (insert image) as string
			SET @sql = N'INSERT INTO dbo.[ImageReferences] (image_data) 
						SELECT * 
						FROM OPENROWSET(BULK ''' + @image_path + ''', SINGLE_BLOB) AS Image;';
			-- execute query string
			EXEC dbo.sp_executesql @sql;
			-- SET @new_image_pk = SCOPE_IDENTITY();
			SET @new_image_pk = IDENT_CURRENT('ImageReferences');
			--update image other values
			UPDATE dbo.[ImageReferences]
			SET image_path = @image_path, 
				image_name = @isbn
			WHERE pk_image = @new_image_pk
			-- update dbo.[Obra] table with image path and fk_image
			UPDATE dbo.[Obra]
			SET fk_imagem = @new_image_pk
			WHERE pk_obra = @pk_obra
		END	
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			COMMIT TRANSACTION;
	END TRY
	BEGIN CATCH
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			ROLLBACK TRANSACTION;
		THROW;
	END CATCH
END
GO

CREATE FUNCTION fn_get_central_nucleo ()
RETURNS INT
AS
BEGIN
	DECLARE @central_nucleo INT
	SELECT @central_nucleo = pk_nucleo
	FROM Nucleo
	WHERE fk_central IS NULL
	RETURN @central_nucleo
END
GO

-- /////////////////////
-- 			6
-- /////////////////////
-- 6. Atualizar o numero de exemplares de determinada obra

CREATE PROCEDURE sp_add_obra
(
	@pk_obra INT,
	@pk_nucleo INT,
	@quantidade INT
)
AS
BEGIN
	DECLARE @initial_trancount INT = @@TRANCOUNT
	IF @initial_trancount = 0
		BEGIN TRANSACTION;
	BEGIN TRY
		--CHECKS:
		-- check if obra exists
		IF NOT EXISTS (SELECT * FROM dbo.[Obra] WHERE pk_obra = @pk_obra)
			THROW 50000, 'Error: obra not found', 1;
		-- check if obra exists in given nucleo
		IF NOT EXISTS (SELECT * FROM dbo.[NucleoObra] WHERE pk_obra = @pk_obra AND pk_nucleo = @pk_nucleo)
			THROW 50000, 'Error: obra not found in given nucleo', 1;
		--UPDATE:
		--update the quantity of obra in nucleo
		UPDATE dbo.[NucleoObra]
		SET quantidade = quantidade + @quantidade
		WHERE pk_obra = @pk_obra AND pk_nucleo = @pk_nucleo
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			COMMIT TRANSACTION;
	END TRY
	BEGIN CATCH
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			ROLLBACK TRANSACTION;
		-- Re-THROW the caught error
		; THROW;
	END CATCH
END
GO

CREATE PROCEDURE sp_rm_obra
(
	@pk_obra INT,
	@pk_nucleo INT,
	@quantidade INT
)
AS
BEGIN
	DECLARE @initial_trancount INT = @@TRANCOUNT
	IF @initial_trancount = 0
		BEGIN TRANSACTION;
	BEGIN TRY
		--CHECKS:
		-- check if obra exists in nucleo,
		IF NOT EXISTS (SELECT * FROM dbo.[NucleoObra] WHERE pk_obra = @pk_obra AND pk_nucleo = @pk_nucleo)
			THROW 50000, 'Error: obra not found in given nucleo', 1;
		-- check if there are enough available copies
		IF (dbo.fn_available_copies(@pk_obra, @pk_nucleo) - @quantidade) < 0
			THROW 50000, 'Error: Insuficient obras for request in given nucleo', 1;
		--UPDATE:
		--update the quantity of obra in nucleo
		UPDATE dbo.[NucleoObra]
		SET quantidade = quantidade - @quantidade
		WHERE pk_obra = @pk_obra AND pk_nucleo = @pk_nucleo
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			COMMIT TRANSACTION;
	END TRY
	BEGIN CATCH
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			ROLLBACK TRANSACTION;
		; THROW;
	END CATCH
END
GO

CREATE FUNCTION fn_available_copies
(
	@pk_obra INT,
	@pk_nucleo INT
)
RETURNS INT
AS
BEGIN
	DECLARE @count INT
	DECLARE @quant INT
	--number of requisitions of obra in nucleo
	SELECT @count = COUNT(*)
	FROM dbo.[Requisicao]
	WHERE pk_obra = @pk_obra AND pk_nucleo = @pk_nucleo AND stat = 'active'
	--number of copies of obra in nucleo
	SELECT @quant = quantidade
	FROM [NucleoObra]
	WHERE pk_obra = @pk_obra AND pk_nucleo = @pk_nucleo
	--return number of available copies
	RETURN @quant - @count
END
GO

-- /////////////////////
-- 			7
-- /////////////////////

-- 7. Transferir um ou mais exemplares de uma obra, de um nucleo para outro

CREATE PROCEDURE sp_transfer_obra
(
	@pk_obra INT,
	@pk_nucleo_origem INT,
	@pk_nucleo_destino INT,
	@quantidade INT
)
AS
BEGIN
	DECLARE @initial_trancount INT = @@TRANCOUNT
	IF @initial_trancount = 0
		BEGIN TRANSACTION;
	BEGIN TRY
		--CHECKS: Are done in lower level procedures
		--remove n copy from origin
		EXEC dbo.sp_rm_obra @pk_obra, @pk_nucleo_origem, @quantidade;
		--adiciona n copy to destination
		IF EXISTS (SELECT * FROM dbo.[NucleoObra] WHERE pk_obra = @pk_obra AND pk_nucleo = @pk_nucleo_destino)
			EXEC dbo.sp_add_obra @pk_obra, @pk_nucleo_destino, @quantidade;
		--In case obra is new in nucleo, create new entry in NucleoObra
		ELSE
			EXEC dbo.sp_add_obra_in_nucleo @pk_obra, @pk_nucleo_destino, @quantidade;
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			COMMIT TRANSACTION;
	END TRY
	BEGIN CATCH
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			ROLLBACK TRANSACTION;
		; THROW;
	END CATCH
END
GO

CREATE PROCEDURE sp_add_obra_in_nucleo
(
	@pk_obra INT,
	@pk_nucleo INT,
	@quantidade INT
)
AS
BEGIN
	--Altho is similar to sp_add_obra, if obra is new in nucleo, create new entry
	DECLARE @initial_trancount INT = @@TRANCOUNT
	IF @initial_trancount = 0
		BEGIN TRANSACTION;
	BEGIN TRY
		--CHECKS:
		-- check if obra exists
		IF NOT EXISTS (SELECT * FROM dbo.[Obra] WHERE pk_obra = @pk_obra)
			THROW 50000, 'Error: obra not found', 1;
		-- check if obra exists in given nucleo 
		IF NOT EXISTS (SELECT * FROM dbo.[NucleoObra] WHERE pk_obra = @pk_obra AND pk_nucleo = @pk_nucleo)
		BEGIN --if not, create new entry
			INSERT INTO dbo.[NucleoObra] (pk_nucleo, pk_obra, quantidade) --quantidade default 1
			VALUES (@pk_nucleo, @pk_obra, @quantidade)
		END
		--UPDATE:
		--update the quantity of obra in nucleo
		ELSE
		BEGIN
			UPDATE dbo.[NucleoObra]
			SET quantidade = quantidade + @quantidade
			WHERE pk_obra = @pk_obra AND pk_nucleo = @pk_nucleo
		END
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			COMMIT TRANSACTION;
	END TRY
	BEGIN CATCH
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			ROLLBACK TRANSACTION;
		-- Re-THROW the caught error
		; THROW;
	END CATCH
END
GO

-- /////////////////////
-- 			8
-- /////////////////////

-- 8. Registar novos leitores


CREATE PROCEDURE sp_insert_leitor
	@nome NVARCHAR(50),
	@morada NVARCHAR(50),
	@telefone NVARCHAR(50),
	@email NVARCHAR(50),
	@user_password NVARCHAR(50),
	@user_role NVARCHAR(50) = 'USER'
	--@STAT IS DEFAULTED in the create table
AS
BEGIN
	DECLARE @initial_trancount INT = @@TRANCOUNT
	IF @initial_trancount = 0
		BEGIN TRANSACTION;
	BEGIN TRY
		--CHECKS:
		--check if user already exists (duplicate)
		IF EXISTS (SELECT * FROM dbo.[Leitor] WHERE email = @email)
			THROW 50000, 'matching leitor login already exists', 1;
		IF EXISTS (SELECT * FROM dbo.[Leitor] WHERE nome_leitor = @nome AND morada = @morada AND telefone = @telefone)
			THROW 50000, 'matching leitor already exists', 1;
		--ACTUAL INSERT:
		INSERT INTO dbo.[Leitor] (nome_leitor, data_inscricao, morada, telefone, email, user_password, user_role)
		VALUES (@nome, GETDATE(), @morada, @telefone, @email, @user_password, @user_role)
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			COMMIT TRANSACTION;
	END TRY
	BEGIN CATCH
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			ROLLBACK TRANSACTION;
		; THROW;
	END CATCH
END
GO

-- /////////////////////
-- 			9
-- /////////////////////

-- 9. Suspender o acesso a requisicões a leitores que tenham procedido a 
-- devoluções atrasadas em mais que três ocasiões

CREATE PROCEDURE sp_suspend_late
(
	@pk_leitor INT
)
AS
BEGIN
	DECLARE @initial_trancount INT = @@TRANCOUNT
	IF @initial_trancount = 0
		BEGIN TRANSACTION;
	BEGIN TRY
		--CHECKS:
		--check if leitor is already suspended
		IF (SELECT stat FROM dbo.[Leitor] WHERE pk_leitor = @pk_leitor) = 'suspended'
			THROW 50000, 'leitor is suspended', 1;
		DECLARE @count_late INT
		--count the number of late returns
		SELECT @count_late = COUNT(*)
		FROM dbo.fn_requisicoes_leitor(@pk_leitor)
		WHERE dbo.fn_check_overtime(data_levantamento, data_devolucao, 15) = 1 AND already_suspend = 0
		--if more than 3 late returns, suspend leitor
		IF @count_late > 3
		BEGIN
			EXEC dbo.sp_leitor_suspend @pk_leitor
			; THROW 50000, 'leitor suspended due to late returns', 1;
		END
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			COMMIT TRANSACTION;
	END TRY
	BEGIN CATCH
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			ROLLBACK TRANSACTION;
		; THROW;
	END CATCH
END
GO

	CREATE PROCEDURE sp_leitor_suspend
(
	@pk_leitor INT
)
AS
BEGIN
	DECLARE @initial_trancount INT = @@TRANCOUNT
	IF @initial_trancount = 0
		BEGIN TRANSACTION;
	BEGIN TRY
		--CHECKS:
		--check if leitor exists
		IF NOT EXISTS (SELECT * FROM dbo.[Leitor] WHERE pk_leitor = @pk_leitor)
			THROW 50000, 'leitor not found', 1;
		--UPDATE:
		--update leitor to suspended
		UPDATE dbo.[Leitor]
		SET stat = 'suspended'
		WHERE pk_leitor = @pk_leitor
		-- ignore books returned and flagged as late
		UPDATE dbp.[Requisicao]
		SET already_suspend = 1
		WHERE pk_leitor = @pk_leitor AND stat = 'returned'
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			COMMIT TRANSACTION;
	END TRY
	BEGIN CATCH
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			ROLLBACK TRANSACTION;
		; THROW;
	END CATCH
END
GO

CREATE FUNCTION fn_check_overtime
(
	@data_levantamento DATE,
	@data_devolucao DATE,
	@dias_limite INT
)
RETURNS BIT
AS
BEGIN
	DECLARE @overtime BIT
	--if obra is not returned, calculate with current date
	IF @data_devolucao IS NULL
		SET @data_devolucao = GETDATE()
	--difftime between devolution and limit date
	DECLARE @diff INT
	SELECT @diff = DATEDIFF(DAY, @data_levantamento, @data_devolucao)
	IF @diff > @dias_limite
		SET @overtime = 1
	ELSE
		SET @overtime = 0
	--return bool for late return
	RETURN @overtime
END
GO

CREATE FUNCTION fn_requisicoes_leitor (@pk_leitor INT)
RETURNS TABLE
AS
RETURN
(
	-- Get all requisitions of a leitor
	SELECT *
	FROM dbo.[Requisicao]
	WHERE pk_leitor = @pk_leitor
)
GO

------ bonus ------ 

CREATE PROCEDURE sp_devolution
(
	@pk_leitor INT,
	@pk_obra INT,
	@nucleo INT
)
AS
BEGIN
	DECLARE @initial_trancount INT = @@TRANCOUNT
	IF @initial_trancount = 0
		BEGIN TRANSACTION;
	BEGIN TRY
		--CHECKS:
		--check if leitor exists
		IF NOT EXISTS (SELECT * FROM dbo.[Leitor] WHERE pk_leitor = @pk_leitor)
			THROW 50000, 'leitor not found', 1;
		--check if requisition exists
		IF NOT EXISTS (SELECT * FROM dbo.[Requisicao] WHERE pk_leitor = @pk_leitor AND pk_obra = @pk_obra AND pk_nucleo = @nucleo)
			THROW 50000, 'requisition not found', 1;
		--UPDATE:
		--update requisition to returned
		UPDATE dbo.[Requisicao]
		SET stat = 'returned', data_devolucao = GETDATE()
		WHERE pk_leitor = @pk_leitor AND pk_obra = @pk_obra AND pk_nucleo = @nucleo
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			COMMIT TRANSACTION;
	END TRY
	BEGIN CATCH
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			ROLLBACK TRANSACTION;
		; THROW;
	END CATCH
END
GO

CREATE PROCEDURE sp_requisition
(
	@pk_leitor INT,
	@pk_obra INT,
	@pk_nucleo INT
)
AS
BEGIN
	DECLARE @initial_trancount INT = @@TRANCOUNT
	IF @initial_trancount = 0
		BEGIN TRANSACTION;
	BEGIN TRY
		--CHECKS:
		--check if leitor exists
		IF NOT EXISTS (SELECT * FROM dbo.[Leitor] WHERE pk_leitor = @pk_leitor)
			THROW 50000, 'leitor not found', 1;
		--check if obra exists
		IF NOT EXISTS (SELECT * FROM dbo.[Obra] WHERE pk_obra = @pk_obra)
			THROW 50000, 'obra not found', 1;
		--check if nucleo exists
		IF NOT EXISTS (SELECT * FROM dbo.[Nucleo] WHERE pk_nucleo = @pk_nucleo)
			THROW 50000, 'nucleo not found', 1;
		--check if leitor is suspended or has 3 more late returns
		EXEC dbo.sp_suspend_late @pk_leitor
		IF (SELECT stat FROM dbo.[Leitor] WHERE pk_leitor = @pk_leitor) = 'suspended'
			THROW 50000, 'leitor is suspended', 1;
		--check if there are available copies
		IF dbo.fn_available_copies(@pk_obra, @pk_nucleo) < 2
			THROW 50000, 'no available copies', 1;
		--check if leitor already requested this obra in any nucleo
		IF EXISTS (SELECT * FROM dbo.[Requisicao] WHERE pk_leitor = @pk_leitor AND pk_obra = @pk_obra)
			THROW 50000, 'leitor already requested this obra', 1;
		IF ((SELECT COUNT(dbo.[Requisicao].pk_obra) FROM dbo.[Requisicao] WHERE pk_leitor = @pk_leitor AND stat = 'borrowed') = 4)
			THROW 50000, 'leitor has 4 requesitions already', 1;
		--ACTUAL INSERT:
		--insert requisition
		INSERT INTO dbo.[Requisicao] (pk_leitor, pk_obra, pk_nucleo, data_levantamento)
		VALUES (@pk_leitor, @pk_obra, @pk_nucleo, GETDATE())
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			COMMIT TRANSACTION;
	END TRY
	BEGIN CATCH
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			ROLLBACK TRANSACTION;
		; THROW;
	END CATCH
END
GO


-- /////////////////////
-- 			10
-- /////////////////////

-- 10. Reativar o acesso a um leitor suspenso

CREATE PROCEDURE sp_leitor_reactivate
(
	@pk_leitor INT
)
AS
BEGIN
	DECLARE @initial_trancount INT = @@TRANCOUNT
	IF @initial_trancount = 0
		BEGIN TRANSACTION;
	BEGIN TRY
		--CHECKS:
		--check if leitor exists
		IF NOT EXISTS (SELECT * FROM dbo.[Leitor] WHERE pk_leitor = @pk_leitor)
			THROW 50000, 'leitor not found', 1;
		--check if leitor is suspended
		IF (SELECT stat FROM dbo.[Leitor] WHERE pk_leitor = @pk_leitor) = 'active'
			THROW 50000, 'leitor already active', 1;
		--UPDATE:
		--update lift suspension
		UPDATE dbo.[Leitor]
		SET stat = 'active'
		WHERE pk_leitor = @pk_leitor
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			COMMIT TRANSACTION;
	END TRY
	BEGIN CATCH
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			ROLLBACK TRANSACTION;
		; THROW;
	END CATCH
END
GO


-- ///////////////////// 
-- 			11 
-- /////////////////////

-- 11. Eliminar leitores que estejam há mais de um ano sem fazer qualquer 
-- requisição, desde que não tenham nenhuma requisição ativa nesse 
-- momento


CREATE PROCEDURE sp_delete_inactive_leitor
(
	@pk_leitor INT
)
AS
BEGIN
	DECLARE @initial_trancount INT = @@TRANCOUNT
	IF @initial_trancount = 0
		BEGIN TRANSACTION;
	BEGIN TRY
		IF NOT EXISTS (SELECT * FROM dbo.[Leitor] WHERE pk_leitor = @pk_leitor)
			THROW 50000, 'Leitor does not exists', 1;
		IF EXISTS (SELECT * FROM dbo.[Requisicao] WHERE pk_leitor = @pk_leitor AND stat = 'borrowed')
			THROW 50000, 'Leitor has active requesitions', 1;
		IF @pk_leitor IN (SELECT pk_leitor FROM dbo.fn_inactive_leitors(DATEADD(YEAR, -1, GETDATE())))
		BEGIN
			-- delete leitor and all his requisitions (saved in history)
			EXEC dbo.sp_del_leitor @pk_leitor
		END
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			COMMIT TRANSACTION;
	END TRY
	BEGIN CATCH
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			ROLLBACK TRANSACTION;
		; THROW;
	END CATCH
END
GO

CREATE PROCEDURE sp_del_leitor 
(
	@pk_leitor INT
)
AS
BEGIN
	DECLARE @initial_trancount INT = @@TRANCOUNT
	IF @initial_trancount = 0
		BEGIN TRANSACTION;
	BEGIN TRY
		--CHECKS:
		--check if leitor exists
		IF NOT EXISTS (SELECT * FROM dbo.[Leitor] WHERE pk_leitor = @pk_leitor)
			THROW 50000, 'leitor not found', 1;
		--check if leitor has requisitions active
		IF EXISTS (SELECT * FROM dbo.[Requisicao] WHERE pk_leitor = @pk_leitor AND stat = 'borrowed')
			THROW 50000, 'leitor has active requesitions', 1;
		--UPDATE:
		-- save all leitor requisitions in history
		EXEC dbo.sp_save_leitor_requesitions @pk_leitor
		-- delete all leitor requisitions
		DELETE FROM dbo.[Requisicao]
		WHERE pk_leitor = @pk_leitor
		-- delete leitor
		DELETE FROM dbo.[Leitor]
		WHERE pk_leitor = @pk_leitor
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			COMMIT TRANSACTION;
	END TRY
	BEGIN CATCH
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			ROLLBACK TRANSACTION;
		; THROW;
	END CATCH
END
GO

CREATE PROCEDURE sp_save_leitor_requesitions
(
	@pk_leitor INT
)
AS
BEGIN
	DECLARE @initial_trancount INT = @@TRANCOUNT
	IF @initial_trancount = 0
		BEGIN TRANSACTION;
	BEGIN TRY
		IF NOT EXISTS (SELECT * FROM [Requisicao] WHERE pk_leitor = @pk_leitor)
			THROW 50000, 'Leitor has no requisitions to save', 1;
		INSERT INTO History (nome_obra, id_obra, nucleo, data_Requisicao, data_devolucao, nome_leitor, id_leitor)
		SELECT dbo.[Obra].nome_obra, dbo.[Requisicao].pk_obra, dbo.[Nucleo].nome_nucleo, dbo.[Requisicao].data_levantamento, dbo.[Requisicao].data_devolucao, dbo.[Leitor].nome_leitor, dbo.[Leitor].pk_leitor
		FROM dbo.[Obra] 
			INNER JOIN dbo.[NucleoObra] ON dbo.[Obra].pk_obra = dbo.[NucleoObra].pk_obra 
			INNER JOIN dbo.[Nucleo] ON dbo.[NucleoObra].pk_nucleo = dbo.[Nucleo].pk_nucleo 
			INNER JOIN dbo.[Requisicao] ON dbo.[Obra].pk_obra = dbo.[Requisicao].pk_obra 
			INNER JOIN dbo.[Leitor] ON dbo.[Requisicao].pk_leitor = dbo.[Leitor].pk_leitor
		-- WHERE dbo.[Requisicao].WHERE pk_leitor = @pk_leitor AND pk_obra = @pk_obra
		WHERE dbo.[Requisicao].pk_leitor = @pk_leitor
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			COMMIT TRANSACTION;
	END TRY
	BEGIN CATCH
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			ROLLBACK TRANSACTION;
		; THROW;
	END CATCH
END
GO

CREATE FUNCTION fn_active_leitors(@date_since DATE) --//TODO change date 
-- Inline TVF: 
RETURNS TABLE
AS
RETURN
(
	SELECT pk_leitor
	FROM dbo.[Requisicao]
	WHERE data_levantamento >= @date_since
	GROUP BY pk_leitor
)
GO

CREATE FUNCTION fn_inactive_leitors(@date_since DATE)
-- Multi-Statement TVF:
RETURNS @InactiveLeitors TABLE (pk_leitor INT)
AS
BEGIN
	INSERT INTO @InactiveLeitors (pk_leitor)
	SELECT pk_leitor
	FROM dbo.[Leitor]
	WHERE pk_leitor NOT IN (SELECT pk_leitor FROM dbo.fn_active_leitors(@date_since))
	RETURN
END
GO



-- Por outro lado, deve ser possivel ao leitor:
-- 1. Fazer o registo de leitor

			--!!!!-- EXEC dbo.sp_insert_leitor 'nome', 'morada', 'telefone', 'email'

-- 2. Cancelar a inscrição, devendo assumir-se que, nesse caso, é feita a
-- devolução de todos os exemplares que possa ter requisitado e não tenha
-- ainda devolvido

CREATE PROCEDURE sp_cancel_leitor
(
	@pk_leitor INT
)
AS
BEGIN
	DECLARE @initial_trancount INT = @@TRANCOUNT
	IF @initial_trancount = 0
	BEGIN TRANSACTION;
	BEGIN TRY
		--check if leitor exists
		IF NOT EXISTS (SELECT * FROM dbo.[Leitor] WHERE pk_leitor = @pk_leitor)
			THROW 50000, 'leitor not found', 1;
		--leitor returns all requisitions
		UPDATE dbo.[Requisicao]
		SET stat = 'returned', data_devolucao = GETDATE()
		WHERE pk_leitor = @pk_leitor
		--delete leitor and saving all requisitions in history
		EXEC dbo.sp_del_leitor @pk_leitor
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			COMMIT TRANSACTION;
	END TRY
	BEGIN CATCH
		IF @initial_trancount = 0 AND @@TRANCOUNT > 0
			ROLLBACK TRANSACTION;
		; THROW;
	END CATCH
END
GO

-- 3. Pesquisar as obras disponiveis, em geral ou por nucleo e/ou por tema

CREATE FUNCTION fn_search_obras(@obra NVARCHAR(50) = NULL)
RETURNS TABLE
AS
RETURN
(
	--select by name, if omited, select all
	SELECT pk_obra
	FROM dbo.[Obra] 
	WHERE (@obra IS NULL OR nome_obra LIKE '%' + @obra+ '%')
	GROUP BY pk_obra
)
GO

CREATE FUNCTION fn_search_obras_genre(@genre NVARCHAR(50) = NULL)
RETURNS TABLE
AS
RETURN
(
	--select all obras from genre, if omited, select all
	SELECT dbo.[Obra].pk_obra
	--
	FROM dbo.[Obra]
		INNER JOIN dbo.[GeneroObra] ON dbo.[Obra].pk_obra = dbo.[GeneroObra].pk_obra 
		INNER JOIN dbo.[Genero] ON dbo.[GeneroObra].pk_genero = dbo.[Genero].pk_genero
	WHERE (@genre IS NULL OR  dbo.[Genero].nome_genero = @genre)
	GROUP BY dbo.[Obra].pk_obra
)
GO

CREATE FUNCTION fn_search_nucleo(@nucleo NVARCHAR(50) = NULL)
RETURNS TABLE
AS
RETURN
(
	--select all obras from nucleo, if omited, select all
	SELECT pk_nucleo
	FROM dbo.[Nucleo]
	WHERE
		@nucleo IS NULL 
		OR (dbo.[Nucleo].nome_nucleo LIKE '%' + @nucleo + '%')
		OR (dbo.[Nucleo].morada LIKE '%' + @nucleo + '%')
	GROUP BY pk_nucleo
)
GO

CREATE PROCEDURE sp_search_obras
(
	@obra NVARCHAR(50) = NULL,
	@genre NVARCHAR(50) = NULL,
	@nucleo NVARCHAR(50) = NULL
)
AS
BEGIN
	BEGIN TRY
	SELECT nome_obra, nome_nucleo, quantidade
	FROM dbo.[NucleoObra] 
		INNER JOIN dbo.[Obra] ON dbo.[NucleoObra].pk_obra = dbo.[Obra].pk_obra 
		INNER JOIN dbo.[Nucleo] ON dbo.[NucleoObra].pk_nucleo = dbo.[Nucleo].pk_nucleo
	WHERE 
	-- general name obra search
	dbo.[NucleoObra].pk_obra IN (SELECT pk_obra FROM dbo.fn_search_obras(@obra))
	-- specific genre search
	AND dbo.[NucleoObra].pk_obra IN (SELECT pk_obra FROM dbo.fn_search_obras_genre(@genre))
	-- specific nucleo search by includin in name or morada
	AND dbo.[NucleoObra].pk_nucleo IN (SELECT pk_nucleo FROM dbo.fn_search_nucleo(@nucleo))
	END TRY
	BEGIN CATCH
		; THROW;
	END CATCH
END
GO

-- 4. Verificar a sua situação, através das seguintes consultas:
-- 4.1. Obras requisitadas, em cada momento, por nucleo, devendo
-- aparecer a indicação 
-- “ATRASO”				caso o prazo já tenha sido ultrapassado,
-- “Devolução URGENTE”	faltem menos de três dias para a devolução ou , respetivamente
-- “Devolver em breve”	faltem cinco dias para a devolução

CREATE PROCEDURE sp_leitor_situation
(
	@pk_leitor INT,
	@nucleo NVARCHAR(50) = NULL
)
AS
BEGIN
	BEGIN TRY
	-- show all requisitions, adding a status message
	SELECT nome_obra, nome_nucleo, data_levantamento, data_devolucao,
		CASE 
			WHEN DATEDIFF(DAY, data_levantamento, GETDATE()) > 15 THEN 'ATRASO'
			WHEN DATEDIFF(DAY, data_levantamento, GETDATE()) > (15 - 3) THEN 'Devolução URGENTE'
			WHEN DATEDIFF(DAY, data_levantamento, GETDATE()) > (15 - 5) THEN 'Devolver em breve'
			ELSE 'Em dia'
		END AS status_message
	FROM dbo.[Requisicao]
		INNER JOIN dbo.[Obra] ON dbo.Requisicao.pk_obra = dbo.[Obra].pk_obra
		INNER JOIN dbo.[Nucleo] ON dbo.[Requisicao].pk_nucleo = dbo.[Nucleo].pk_nucleo
	WHERE (dbo.[Requisicao].pk_leitor = @pk_leitor)
		AND (stat = 'borrowed')
	-- dinamic filtering where clause can be omited (null)
		AND (dbo.[Requisicao].pk_nucleo IN (SELECT pk_nucleo FROM dbo.fn_search_nucleo(@nucleo)));
	END TRY
	BEGIN CATCH
		;THROW;
	END CATCH
END
GO

-- 4.2. Histórico de todas as obras requisitadas, podendo opcionalmente
-- indicar um agrupamento por nucleo e/ou um intervalo de datas

CREATE PROCEDURE sp_history
(
	@pk_leitor INT, 
	@pk_nucleo INT = NULL, 
	@start_date DATE = NULL, 
	@end_date DATE = NULL
)
AS
BEGIN
	BEGIN TRY
	SELECT nome_obra, nome_nucleo, stat, data_levantamento, data_devolucao
	FROM dbo.[Requisicao]
		INNER JOIN dbo.[Obra] ON dbo.Requisicao.pk_obra = dbo.[Obra].pk_obra
		INNER JOIN dbo.[Nucleo] ON dbo.[Requisicao].pk_nucleo = dbo.[Nucleo].pk_nucleo
	WHERE (dbo.[Requisicao].pk_leitor = @pk_leitor)
		AND (@pk_nucleo IS NULL OR dbo.[Requisicao].pk_nucleo = @pk_nucleo)
		AND (@start_date IS NULL OR data_levantamento >= @start_date)
		AND (@end_date IS NULL OR data_levantamento <= @end_date);
	END TRY
	BEGIN CATCH
		THROW;
	END CATCH
END
GO