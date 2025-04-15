USE master;
GO

-- Drop the database if it exists
IF DB_ID('Projecto') IS NOT NULL
BEGIN
    -- Set the database to single-user mode and roll back any open transactions
    ALTER DATABASE [Projecto] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    -- Drop the database
    DROP DATABASE [Projecto];
END
GO

-- Recreate the database
CREATE DATABASE [Projecto];
GO

USE [Projecto];
GO

-- Drop tables if they exist

IF OBJECT_ID('dbo.History', 'U') IS NOT NULL
    DROP TABLE dbo.History;
IF OBJECT_ID('dbo.ImageReferences', 'U') IS NOT NULL
    DROP TABLE dbo.ImageReferences;
IF OBJECT_ID('dbo.Obra', 'U') IS NOT NULL
    DROP TABLE dbo.Obra;
IF OBJECT_ID('dbo.Genero', 'U') IS NOT NULL
    DROP TABLE dbo.Genero;
IF OBJECT_ID('dbo.GeneroObra', 'U') IS NOT NULL
    DROP TABLE dbo.GeneroObra;
IF OBJECT_ID('dbo.Autor', 'U') IS NOT NULL
    DROP TABLE dbo.Autor;
IF OBJECT_ID('dbo.AutorObra', 'U') IS NOT NULL
    DROP TABLE dbo.AutorObra;
IF OBJECT_ID('dbo.Leitor', 'U') IS NOT NULL
    DROP TABLE dbo.Leitor;
IF OBJECT_ID('dbo.Requisicao', 'U') IS NOT NULL
    DROP TABLE dbo.Requisicao;
IF OBJECT_ID('dbo.Nucleo', 'U') IS NOT NULL
    DROP TABLE dbo.Nucleo;
IF OBJECT_ID('dbo.NucleoObra', 'U') IS NOT NULL
    DROP TABLE dbo.NucleoObra;

-- Create ImageReferences table
CREATE TABLE ImageReferences (
    pk_image INT IDENTITY(1,1) PRIMARY KEY,
    image_name NVARCHAR(50),
    image_path NVARCHAR(255), -- file path
    image_data VARBINARY(MAX)
);
-- Create Obra table
CREATE TABLE Obra (
    pk_obra INT IDENTITY(1,1) PRIMARY KEY,
    nome_obra NVARCHAR(50) NOT NULL,
    ISBN NVARCHAR(50),
    editora NVARCHAR(50),
    ano INT NOT NULL,
    fk_imagem INT NULL,
    FOREIGN KEY (fk_imagem) REFERENCES ImageReferences(pk_image)
);
-- Create Genero table
CREATE TABLE Genero (
    pk_genero INT IDENTITY(1,1) PRIMARY KEY,
    nome_genero NVARCHAR(50) NOT NULL
);
-- Create GeneroObra table for many-to-many relationship
CREATE TABLE GeneroObra (
    pk_genero INT NOT NULL,
    pk_obra INT NOT NULL,
    PRIMARY KEY (pk_genero, pk_obra),
    FOREIGN KEY (pk_genero) REFERENCES Genero(pk_genero),
    FOREIGN KEY (pk_obra) REFERENCES Obra(pk_obra)
);
-- Create Autor table
CREATE TABLE Autor (
    pk_autor INT IDENTITY(1,1) PRIMARY KEY,
    nome_autor NVARCHAR(50) NOT NULL
);
-- Create AutorObra table for many-to-many relationship
CREATE TABLE AutorObra (
    pk_autor INT NOT NULL,
    pk_obra INT NOT NULL,
    PRIMARY KEY (pk_autor, pk_obra),
    FOREIGN KEY (pk_autor) REFERENCES Autor(pk_autor),
    FOREIGN KEY (pk_obra) REFERENCES Obra(pk_obra)
);
-- Create Leitor table
CREATE TABLE Leitor (
    pk_leitor INT IDENTITY(1,1) PRIMARY KEY,
	data_inscricao DATE DEFAULT GETDATE(),
    user_password NVARCHAR(50) NOT NULL,
	user_role NVARCHAR(50) DEFAULT 'USER',
    stat NVARCHAR(50) DEFAULT 'active',
    nome_leitor NVARCHAR(50) NOT NULL,
    telefone NVARCHAR(20) DEFAULT NULL,
    email NVARCHAR(50) DEFAULT NULL,
    morada NVARCHAR(50) DEFAULT NULL
);

-- Create Nucleo table
CREATE TABLE Nucleo (
    pk_nucleo INT IDENTITY(1,1) PRIMARY KEY,
    fk_central INT,
    nome_nucleo NVARCHAR(50) NOT NULL,
    morada NVARCHAR(50)  DEFAULT NULL,
    telefone NVARCHAR(20)  DEFAULT NULL,
    FOREIGN KEY (fk_central) REFERENCES Nucleo(pk_nucleo)
);
-- Create NucleoObra table
CREATE TABLE NucleoObra (
    pk_nucleo INT NOT NULL,
    pk_obra INT NOT NULL,
    quantidade INT NOT NULL,
    PRIMARY KEY (pk_nucleo, pk_obra),
    FOREIGN KEY (pk_nucleo) REFERENCES Nucleo(pk_nucleo),
    FOREIGN KEY (pk_obra) REFERENCES Obra(pk_obra)
);

-- Create Requisicao table for one-to-many relationship
CREATE TABLE Requisicao (
    pk_leitor INT NOT NULL,
    pk_obra INT NOT NULL,
    pk_nucleo INT NOT NULL,
    stat NVARCHAR(50) DEFAULT 'borrowed',
    data_levantamento DATE,
    data_devolucao DATE DEFAULT NULL,
    already_suspend BIT DEFAULT 0,
    PRIMARY KEY (pk_leitor, pk_obra, pk_nucleo),
    FOREIGN KEY (pk_leitor) REFERENCES Leitor(pk_leitor),
    FOREIGN KEY (pk_obra) REFERENCES Obra(pk_obra)
);

-- Create History table
CREATE TABLE History (
    pk_log INT IDENTITY(1,1) PRIMARY KEY,
    nome_obra NVARCHAR(50),
    id_obra INT,
    nucleo NVARCHAR(50),
    data_requisicao DATE,
    data_devolucao DATE,
    nome_leitor NVARCHAR(50),
    id_leitor INT
);

-- Insert values for testing
-- Insert into ImageReferences
INSERT INTO ImageReferences (image_name, image_path, image_data) VALUES
('Image1', 'path/to/image1.jpg', NULL),
('Image2', 'path/to/image2.jpg', NULL),
('Image3', 'path/to/image3.jpg', NULL);

-- Insert into Obra with different editions and random data
INSERT INTO Obra (nome_obra, ISBN, editora, ano, fk_imagem) VALUES
('The Great Gatsby', '9780743273565', 'Scribner', 1925, 1),
('To Kill a Mockingbird', '9780061120084', 'J.B. Lippincott & Co.', 1960, 2),
('1984', '9780451524935', 'Secker & Warburg', 1949, 3),
('Pride and Prejudice', '9781503290563', 'T. Egerton', 1813, NULL),
('The Catcher in the Rye', '9780316769488', 'Little, Brown and Company', 1951, NULL),
('The Hobbit', '9780547928227', 'George Allen & Unwin', 1937, NULL),
('Moby-Dick', '9781503280786', 'Harper & Brothers', 1851, NULL),
('War and Peace', '9780199232765', 'Oxford University Press', 1869, NULL),
('The Odyssey', '9780140268867', 'Penguin Classics', -800, NULL),
('Ulysses', '9780199535675', 'Oxford University Press', 1922, NULL),
('The Divine Comedy', '9780140448955', 'Penguin Classics', 1320, NULL),
('Hamlet', '9780140714548', 'Penguin Classics', 1603, NULL),
('The Iliad', '9780140275360', 'Penguin Classics', -750, NULL),
('Don Quixote', '9780060934347', 'Harper Perennial', 1605, NULL),
('One Hundred Years of Solitude', '9780060883287', 'Harper Perennial', 1967, NULL);

-- Insert into Genero
INSERT INTO Genero (nome_genero) VALUES
('Fiction'),
('Classic'),
('Science Fiction'),
('Fantasy'),
('Adventure'),
('Historical'),
('Drama'),
('Epic');

-- Insert into GeneroObra (one obra can have as many as 3 genres)
INSERT INTO GeneroObra (pk_genero, pk_obra) VALUES
(1, 1),
(2, 1),
(6, 1),
(1, 2),
(2, 2),
(7, 2),
(1, 3),
(3, 3),
(5, 3),
(2, 4),
(7, 4),
(8, 4),
(1, 5),
(2, 5),
(7, 5),
(4, 6),
(5, 6),
(8, 6),
(2, 7),
(5, 7),
(6, 7),
(2, 8),
(6, 8),
(7, 8),
(2, 9),
(8, 9),
(1, 10),
(2, 10),
(7, 10),
(2, 11),
(7, 11),
(8, 11),
(2, 12),
(7, 12),
(8, 12),
(2, 13),
(8, 13),
(2, 14),
(7, 14),
(1, 15),
(2, 15),
(7, 15);

-- Insert into Leitor
INSERT INTO Leitor (nome_leitor, telefone, email, morada, stat, user_password, user_role) VALUES
('John Doe', '1234567890', 'john.doe@example.com', '123 Main St', 'active', 1234, 'ADMIN'),
('Jane Smith', '0987654321', 'jane.smith@example.com', '456 Elm St', 'Inactive', 1234, 'USER'),
('Alice Johnson', '1122334455', 'alice.johnson@example.com', '789 Oak St', 'active', 1234, 'USER'),
('Bob Brown', '5566778899', 'bob.brown@example.com', '101 Pine St', 'active', 1234, 'USER'),
('Charlie Black', '6677889900', 'charlie.black@example.com', '202 Maple St', 'active', 1234, 'USER'),
('Diana White', '7788990011', 'diana.white@example.com', '303 Birch St', 'active', 1234, 'USER'),
('New Leitor', '1234567890', 'new.leitor@example.com', '456 New St', 'active', 1234, 'USER');

-- Insert into Nucleo
INSERT INTO Nucleo (nome_nucleo, morada, telefone, fk_central) VALUES
('Central Library', '123 Library St', '1234567890', NULL),
('Westside Branch', '456 West St', '0987654321', 1),
('Eastside Branch', '789 East St', '1122334455', 1);


-- Insert into Requisicao with data_levantamento and data_devolucao
-- Insert into Requisicao with data_levantamento and data_devolucao
INSERT INTO Requisicao (pk_leitor, pk_obra, pk_nucleo, stat, data_levantamento, data_devolucao) VALUES
(1, 7, 1, 'borrowed', '2024-10-05', NULL),
(1, 13, 2, 'borrowed', '2024-06-20', NULL),
(1, 4, 3, 'borrowed', '2024-07-15', NULL),
(1, 10, 1, 'borrowed', '2024-08-15', NULL),
(1, 1, 2, 'returned', '2022-12-01', '2022-12-15'),
(1, 2, 3, 'returned', '2023-01-01', '2023-01-15'),
(1, 11, 1, 'returned', '2023-10-01', '2023-10-15'),
(1, 3, 2, 'returned', '2023-02-01', '2023-02-16'),
(1, 7, 3, 'returned', '2024-05-30', '2024-06-14'),
(2, 11, 1, 'borrowed', '2024-08-20', NULL),
(2, 2, 2, 'borrowed', '2024-09-15', NULL),
(2, 8, 3, 'borrowed', '2024-10-10', NULL),
(2, 14, 1, 'borrowed', '2024-06-25', NULL),
(2, 1, 2, 'returned', '2022-12-10', '2022-12-25'),
(2, 3, 3, 'returned', '2023-02-10', '2023-02-25'),
(2, 12, 1, 'returned', '2023-11-01', '2023-11-16'),
(2, 8, 2, 'returned', '2024-05-31', '2024-06-15'),
(2, 5, 3, 'returned', '2024-07-20', '2024-08-04'),
(3, 3, 1, 'returned', '2023-02-15', '2023-03-02'),
(3, 9, 2, 'returned', '2024-06-01', '2024-06-16'),
(3, 15, 3, 'returned', '2024-06-30', '2024-07-15'),
(3, 6, 1, 'returned', '2024-07-25', '2024-08-09'),
(3, 12, 2, 'returned', '2024-08-25', '2024-09-09'),
(3, 3, 3, 'returned', '2024-09-20', '2024-10-05'),
(3, 9, 1, 'returned', '2024-10-15', '2024-10-30'),
(3, 13, 2, 'borrowed', '2024-08-30', NULL),
(4, 4, 3, 'borrowed', '2024-09-25', NULL),
(4, 10, 1, 'borrowed', '2024-06-05', NULL),
(4, 1, 2, 'borrowed', '2024-07-01', NULL),
(4, 2, 3, 'returned', '2023-01-20', '2023-02-04'),
(4, 14, 1, 'returned', '2024-01-01', '2024-01-16'),
(4, 3, 2, 'returned', '2023-02-20', '2023-03-07'),
(4, 7, 1, 'returned', '2024-08-01', '2024-08-16'),
(5, 5, 2, 'borrowed', '2024-09-30', NULL),
(5, 14, 3, 'borrowed', '2024-09-01', NULL),
(5, 2, 1, 'borrowed', '2024-07-05', NULL),
(5, 11, 2, 'borrowed', '2024-06-10', NULL),
(5, 4, 3, 'returned', '2023-03-01', '2023-03-16'),
(5, 5, 1, 'returned', '2023-04-01', '2023-04-16'),
(5, 6, 2, 'returned', '2023-05-01', '2023-05-16'),
(5, 15, 3, 'returned', '2024-02-01', '2024-02-16'),
(5, 8, 1, 'returned', '2024-08-05', '2024-08-20'),
(6, 9, 2, 'returned', '2023-08-01', '2023-08-16'),
(6, 6, 3, 'returned', '2024-05-25', '2024-06-09'),
(6, 12, 1, 'returned', '2024-06-15', '2024-06-30'),
(6, 3, 2, 'returned', '2024-07-10', '2024-07-25'),
(6, 9, 3, 'returned', '2024-08-10', '2024-08-25'),
(6, 15, 1, 'returned', '2024-09-05', '2024-09-20'),
(6, 6, 2, 'returned', '2024-10-01', '2024-10-16'),
(7, 1, 1, 'returned', '2022-10-15', '2024-10-30');

-- Insert into NucleoObra (one nucleo can have multiple obras and obras can be repeated across nucleos)
INSERT INTO NucleoObra (pk_nucleo, pk_obra, quantidade) VALUES
(1, 1, 10),
(1, 2, 5),
(1, 3, 15),
(1, 4, 20),
(2, 2, 10),
(2, 3, 5),
(2, 4, 15),
(2, 5, 20),
(3, 3, 10),
(3, 4, 5),
(3, 5, 15),
(3, 6, 20),
(1, 5, 10),
(2, 6, 5),
(3, 7, 15),
(1, 8, 10),
(2, 9, 5),
(3, 10, 15),
(1, 11, 20),
(2, 12, 10);

-- Insert into History
INSERT INTO [History] (nome_obra, id_obra, nucleo, data_requisicao, data_devolucao, nome_leitor, id_leitor) VALUES
('The Great Gatsby', 1, 'Central Library', '2023-01-01', '2023-01-15', 'John Doe', 1),
('To Kill a Mockingbird', 2, 'Westside Branch', '2023-02-01', '2023-02-15', 'Jane Smith', 2),
('1984', 3, 'Eastside Branch', '2023-03-01', '2023-03-15', 'Alice Johnson', 3);
