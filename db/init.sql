-- Project Checking schema and seed data.
-- Idempotent script: safe to run multiple times.

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Roles')
BEGIN
    CREATE TABLE Roles
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(50) NOT NULL UNIQUE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Login NVARCHAR(100) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(200) NOT NULL,
        RoleId INT NOT NULL REFERENCES Roles(Id),
        FullName NVARCHAR(200) NOT NULL,
        Phone NVARCHAR(40) NULL,
        BirthDate DATE NULL,
        RegistrationDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Activities')
BEGIN
    CREATE TABLE Activities
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL UNIQUE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Qualifications')
BEGIN
    CREATE TABLE Qualifications
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL UNIQUE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ActivityQualifications')
BEGIN
    CREATE TABLE ActivityQualifications
    (
        ActivityId INT NOT NULL REFERENCES Activities(Id),
        QualificationId INT NOT NULL REFERENCES Qualifications(Id),
        PRIMARY KEY (ActivityId, QualificationId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Leaders')
BEGIN
    CREATE TABLE Leaders
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        FullName NVARCHAR(200) NOT NULL,
        Phone NVARCHAR(40) NULL,
        Email NVARCHAR(200) NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'LeaderQualifications')
BEGIN
    CREATE TABLE LeaderQualifications
    (
        LeaderId INT NOT NULL REFERENCES Leaders(Id),
        QualificationId INT NOT NULL REFERENCES Qualifications(Id),
        PRIMARY KEY (LeaderId, QualificationId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Workers')
BEGIN
    CREATE TABLE Workers
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        FullName NVARCHAR(200) NOT NULL,
        Phone NVARCHAR(40) NULL,
        Email NVARCHAR(200) NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT N'свободен'
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Projects')
BEGIN
    CREATE TABLE Projects
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ActivityId INT NOT NULL REFERENCES Activities(Id),
        Customer NVARCHAR(300) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        LeaderId INT NULL REFERENCES Leaders(Id),
        CreationDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        Status NVARCHAR(20) NOT NULL DEFAULT N'в очереди',
        CompletionDate DATETIME2 NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ProjectWorkers')
BEGIN
    CREATE TABLE ProjectWorkers
    (
        ProjectId INT NOT NULL REFERENCES Projects(Id),
        WorkerId INT NOT NULL REFERENCES Workers(Id),
        PRIMARY KEY (ProjectId, WorkerId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ProjectHistory')
BEGIN
    CREATE TABLE ProjectHistory
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ProjectId INT NOT NULL REFERENCES Projects(Id),
        WorkerId INT NULL REFERENCES Workers(Id),
        LeaderId INT NULL REFERENCES Leaders(Id),
        JoinedAt DATETIME2 NOT NULL,
        LeftAt DATETIME2 NULL
    );
END
GO

-- ============================================================
-- Seed data
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = N'администратор') INSERT INTO Roles (Name) VALUES (N'администратор');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = N'менеджер') INSERT INTO Roles (Name) VALUES (N'менеджер');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = N'руководитель проекта') INSERT INTO Roles (Name) VALUES (N'руководитель проекта');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = N'работник') INSERT INTO Roles (Name) VALUES (N'работник');
GO

DECLARE @RoleAdmin INT = (SELECT Id FROM Roles WHERE Name = N'администратор');
DECLARE @RoleManager INT = (SELECT Id FROM Roles WHERE Name = N'менеджер');
DECLARE @RoleLeader INT = (SELECT Id FROM Roles WHERE Name = N'руководитель проекта');
DECLARE @RoleWorker INT = (SELECT Id FROM Roles WHERE Name = N'работник');

-- Default users; passwords are SHA-256 hex of the value below.
-- admin: admin123 -> 240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9
-- manager1: manager123 -> 866485796cfa8d7c0cf7111640205b83076433547577511d81f8030ae99ecea5
-- leader1: leader123 -> 6b4b7f0b81d0b3494dd853bc45c0605fa99125c93de8a9850cbc62b2f6d52d13
-- worker1: worker123 -> 312bba6ac1c4274943d7d3c1f346e8e27310c731e407ce5592d82f0d101fbff1

IF NOT EXISTS (SELECT 1 FROM Users WHERE Login = N'admin')
BEGIN
    INSERT INTO Users (Login, PasswordHash, RoleId, FullName, Phone, BirthDate)
    VALUES (N'admin', N'240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9',
            @RoleAdmin, N'Иванов Иван Иванович', N'+7 (900) 000-00-01', '1985-05-10');
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE Login = N'manager1')
BEGIN
    INSERT INTO Users (Login, PasswordHash, RoleId, FullName, Phone, BirthDate)
    VALUES (N'manager1', N'866485796cfa8d7c0cf7111640205b83076433547577511d81f8030ae99ecea5',
            @RoleManager, N'Петров Пётр Петрович', N'+7 (900) 000-00-02', '1990-03-15');
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE Login = N'leader1')
BEGIN
    INSERT INTO Users (Login, PasswordHash, RoleId, FullName, Phone, BirthDate)
    VALUES (N'leader1', N'6b4b7f0b81d0b3494dd853bc45c0605fa99125c93de8a9850cbc62b2f6d52d13',
            @RoleLeader, N'Сидоров Сидор Сидорович', N'+7 (900) 000-00-03', '1980-07-22');
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE Login = N'worker1')
BEGIN
    INSERT INTO Users (Login, PasswordHash, RoleId, FullName, Phone, BirthDate)
    VALUES (N'worker1', N'312bba6ac1c4274943d7d3c1f346e8e27310c731e407ce5592d82f0d101fbff1',
            @RoleWorker, N'Кузнецов Алексей Викторович', N'+7 (900) 000-00-04', '1995-11-30');
END

-- Repair: fix password hashes for existing default users created with older incorrect hashes.
-- Only updates rows that still hold the known-bad seed hash, so any password changed by an
-- administrator through the application is left untouched.
UPDATE Users SET PasswordHash = N'866485796cfa8d7c0cf7111640205b83076433547577511d81f8030ae99ecea5'
    WHERE Login = N'manager1' AND PasswordHash = N'4abdfb9b56b4a4bb22cb14f9aa667f48bbe89c2008ac26b78cc6db8d44a86c3a';
UPDATE Users SET PasswordHash = N'6b4b7f0b81d0b3494dd853bc45c0605fa99125c93de8a9850cbc62b2f6d52d13'
    WHERE Login = N'leader1'  AND PasswordHash = N'7ae0e93e1adf65edd9d017d8b09b3ca06773ce03ce04f43d51e57e1d3fbb19a4';
UPDATE Users SET PasswordHash = N'312bba6ac1c4274943d7d3c1f346e8e27310c731e407ce5592d82f0d101fbff1'
    WHERE Login = N'worker1'  AND PasswordHash = N'7d50c83ee2ddbe9389c91d11c61eeaaadce8da7be3884cfb6cfa12c1054af2fc';
GO

-- Activities
IF NOT EXISTS (SELECT 1 FROM Activities WHERE Name = N'инженерные изыскания') INSERT INTO Activities (Name) VALUES (N'инженерные изыскания');
IF NOT EXISTS (SELECT 1 FROM Activities WHERE Name = N'проектирование автодорог') INSERT INTO Activities (Name) VALUES (N'проектирование автодорог');
IF NOT EXISTS (SELECT 1 FROM Activities WHERE Name = N'проектирование мостовых сооружений') INSERT INTO Activities (Name) VALUES (N'проектирование мостовых сооружений');
IF NOT EXISTS (SELECT 1 FROM Activities WHERE Name = N'сметная документация') INSERT INTO Activities (Name) VALUES (N'сметная документация');
IF NOT EXISTS (SELECT 1 FROM Activities WHERE Name = N'авторский надзор') INSERT INTO Activities (Name) VALUES (N'авторский надзор');
IF NOT EXISTS (SELECT 1 FROM Activities WHERE Name = N'обследование мостов') INSERT INTO Activities (Name) VALUES (N'обследование мостов');
IF NOT EXISTS (SELECT 1 FROM Activities WHERE Name = N'трёхмерное моделирование зданий и сооружений') INSERT INTO Activities (Name) VALUES (N'трёхмерное моделирование зданий и сооружений');
GO

-- Qualifications
IF NOT EXISTS (SELECT 1 FROM Qualifications WHERE Name = N'инженер-изыскатель') INSERT INTO Qualifications (Name) VALUES (N'инженер-изыскатель');
IF NOT EXISTS (SELECT 1 FROM Qualifications WHERE Name = N'инженер-проектировщик') INSERT INTO Qualifications (Name) VALUES (N'инженер-проектировщик');
IF NOT EXISTS (SELECT 1 FROM Qualifications WHERE Name = N'инженер-сметчик') INSERT INTO Qualifications (Name) VALUES (N'инженер-сметчик');
IF NOT EXISTS (SELECT 1 FROM Qualifications WHERE Name = N'главный архитектор') INSERT INTO Qualifications (Name) VALUES (N'главный архитектор');
IF NOT EXISTS (SELECT 1 FROM Qualifications WHERE Name = N'инженер по обследованию мостов') INSERT INTO Qualifications (Name) VALUES (N'инженер по обследованию мостов');
IF NOT EXISTS (SELECT 1 FROM Qualifications WHERE Name = N'инженер по трёхмерному моделированию') INSERT INTO Qualifications (Name) VALUES (N'инженер по трёхмерному моделированию');
GO

-- Activity <-> Qualification map (per ТЗ)
DECLARE @aIzysk INT = (SELECT Id FROM Activities WHERE Name = N'инженерные изыскания');
DECLARE @aRoads INT = (SELECT Id FROM Activities WHERE Name = N'проектирование автодорог');
DECLARE @aBridges INT = (SELECT Id FROM Activities WHERE Name = N'проектирование мостовых сооружений');
DECLARE @aSmeta INT = (SELECT Id FROM Activities WHERE Name = N'сметная документация');
DECLARE @aSuper INT = (SELECT Id FROM Activities WHERE Name = N'авторский надзор');
DECLARE @aBridgeInsp INT = (SELECT Id FROM Activities WHERE Name = N'обследование мостов');
DECLARE @aBim INT = (SELECT Id FROM Activities WHERE Name = N'трёхмерное моделирование зданий и сооружений');

DECLARE @qIzysk INT = (SELECT Id FROM Qualifications WHERE Name = N'инженер-изыскатель');
DECLARE @qProj INT = (SELECT Id FROM Qualifications WHERE Name = N'инженер-проектировщик');
DECLARE @qSmet INT = (SELECT Id FROM Qualifications WHERE Name = N'инженер-сметчик');
DECLARE @qArch INT = (SELECT Id FROM Qualifications WHERE Name = N'главный архитектор');
DECLARE @qBridge INT = (SELECT Id FROM Qualifications WHERE Name = N'инженер по обследованию мостов');
DECLARE @qBim INT = (SELECT Id FROM Qualifications WHERE Name = N'инженер по трёхмерному моделированию');

IF NOT EXISTS (SELECT 1 FROM ActivityQualifications WHERE ActivityId=@aIzysk AND QualificationId=@qIzysk)
    INSERT INTO ActivityQualifications (ActivityId, QualificationId) VALUES (@aIzysk, @qIzysk);
IF NOT EXISTS (SELECT 1 FROM ActivityQualifications WHERE ActivityId=@aRoads AND QualificationId=@qProj)
    INSERT INTO ActivityQualifications (ActivityId, QualificationId) VALUES (@aRoads, @qProj);
IF NOT EXISTS (SELECT 1 FROM ActivityQualifications WHERE ActivityId=@aBridges AND QualificationId=@qProj)
    INSERT INTO ActivityQualifications (ActivityId, QualificationId) VALUES (@aBridges, @qProj);
IF NOT EXISTS (SELECT 1 FROM ActivityQualifications WHERE ActivityId=@aSmeta AND QualificationId=@qSmet)
    INSERT INTO ActivityQualifications (ActivityId, QualificationId) VALUES (@aSmeta, @qSmet);
IF NOT EXISTS (SELECT 1 FROM ActivityQualifications WHERE ActivityId=@aSuper AND QualificationId=@qArch)
    INSERT INTO ActivityQualifications (ActivityId, QualificationId) VALUES (@aSuper, @qArch);
IF NOT EXISTS (SELECT 1 FROM ActivityQualifications WHERE ActivityId=@aBridgeInsp AND QualificationId=@qBridge)
    INSERT INTO ActivityQualifications (ActivityId, QualificationId) VALUES (@aBridgeInsp, @qBridge);
IF NOT EXISTS (SELECT 1 FROM ActivityQualifications WHERE ActivityId=@aBim AND QualificationId=@qBim)
    INSERT INTO ActivityQualifications (ActivityId, QualificationId) VALUES (@aBim, @qBim);
GO

-- Sample leaders
IF NOT EXISTS (SELECT 1 FROM Leaders WHERE FullName = N'Сидоров Сидор Сидорович')
BEGIN
    INSERT INTO Leaders (FullName, Phone, Email) VALUES (N'Сидоров Сидор Сидорович', N'+7 (900) 000-00-03', N'sidorov@example.com');
END
IF NOT EXISTS (SELECT 1 FROM Leaders WHERE FullName = N'Михайлов Михаил Михайлович')
BEGIN
    INSERT INTO Leaders (FullName, Phone, Email) VALUES (N'Михайлов Михаил Михайлович', N'+7 (900) 000-01-01', N'mihailov@example.com');
END
IF NOT EXISTS (SELECT 1 FROM Leaders WHERE FullName = N'Орлов Олег Олегович')
BEGIN
    INSERT INTO Leaders (FullName, Phone, Email) VALUES (N'Орлов Олег Олегович', N'+7 (900) 000-01-02', N'orlov@example.com');
END
GO

DECLARE @l1 INT = (SELECT Id FROM Leaders WHERE FullName = N'Сидоров Сидор Сидорович');
DECLARE @l2 INT = (SELECT Id FROM Leaders WHERE FullName = N'Михайлов Михаил Михайлович');
DECLARE @l3 INT = (SELECT Id FROM Leaders WHERE FullName = N'Орлов Олег Олегович');

DECLARE @qIzysk INT = (SELECT Id FROM Qualifications WHERE Name = N'инженер-изыскатель');
DECLARE @qProj INT = (SELECT Id FROM Qualifications WHERE Name = N'инженер-проектировщик');
DECLARE @qSmet INT = (SELECT Id FROM Qualifications WHERE Name = N'инженер-сметчик');
DECLARE @qArch INT = (SELECT Id FROM Qualifications WHERE Name = N'главный архитектор');
DECLARE @qBridge INT = (SELECT Id FROM Qualifications WHERE Name = N'инженер по обследованию мостов');
DECLARE @qBim INT = (SELECT Id FROM Qualifications WHERE Name = N'инженер по трёхмерному моделированию');

IF NOT EXISTS (SELECT 1 FROM LeaderQualifications WHERE LeaderId=@l1 AND QualificationId=@qIzysk)
    INSERT INTO LeaderQualifications VALUES (@l1, @qIzysk);
IF NOT EXISTS (SELECT 1 FROM LeaderQualifications WHERE LeaderId=@l1 AND QualificationId=@qProj)
    INSERT INTO LeaderQualifications VALUES (@l1, @qProj);
IF NOT EXISTS (SELECT 1 FROM LeaderQualifications WHERE LeaderId=@l2 AND QualificationId=@qSmet)
    INSERT INTO LeaderQualifications VALUES (@l2, @qSmet);
IF NOT EXISTS (SELECT 1 FROM LeaderQualifications WHERE LeaderId=@l2 AND QualificationId=@qArch)
    INSERT INTO LeaderQualifications VALUES (@l2, @qArch);
IF NOT EXISTS (SELECT 1 FROM LeaderQualifications WHERE LeaderId=@l3 AND QualificationId=@qBridge)
    INSERT INTO LeaderQualifications VALUES (@l3, @qBridge);
IF NOT EXISTS (SELECT 1 FROM LeaderQualifications WHERE LeaderId=@l3 AND QualificationId=@qBim)
    INSERT INTO LeaderQualifications VALUES (@l3, @qBim);
GO

-- Workers
IF NOT EXISTS (SELECT 1 FROM Workers WHERE FullName = N'Кузнецов Алексей Викторович')
    INSERT INTO Workers (FullName, Phone, Email, Status) VALUES (N'Кузнецов Алексей Викторович', N'+7 (900) 000-00-04', N'kuznetsov@example.com', N'свободен');
IF NOT EXISTS (SELECT 1 FROM Workers WHERE FullName = N'Соколов Дмитрий Сергеевич')
    INSERT INTO Workers (FullName, Phone, Email, Status) VALUES (N'Соколов Дмитрий Сергеевич', N'+7 (900) 000-02-01', N'sokolov@example.com', N'свободен');
IF NOT EXISTS (SELECT 1 FROM Workers WHERE FullName = N'Морозова Анна Игоревна')
    INSERT INTO Workers (FullName, Phone, Email, Status) VALUES (N'Морозова Анна Игоревна', N'+7 (900) 000-02-02', N'morozova@example.com', N'свободен');
IF NOT EXISTS (SELECT 1 FROM Workers WHERE FullName = N'Лебедев Артём Александрович')
    INSERT INTO Workers (FullName, Phone, Email, Status) VALUES (N'Лебедев Артём Александрович', N'+7 (900) 000-02-03', N'lebedev@example.com', N'свободен');
IF NOT EXISTS (SELECT 1 FROM Workers WHERE FullName = N'Новикова Светлана Юрьевна')
    INSERT INTO Workers (FullName, Phone, Email, Status) VALUES (N'Новикова Светлана Юрьевна', N'+7 (900) 000-02-04', N'novikova@example.com', N'свободен');
GO

-- Sample projects
DECLARE @aRoads2 INT = (SELECT Id FROM Activities WHERE Name = N'проектирование автодорог');
DECLARE @aIzysk2 INT = (SELECT Id FROM Activities WHERE Name = N'инженерные изыскания');
DECLARE @aBridgeInsp2 INT = (SELECT Id FROM Activities WHERE Name = N'обследование мостов');
DECLARE @leadSidorov INT = (SELECT Id FROM Leaders WHERE FullName = N'Сидоров Сидор Сидорович');
DECLARE @leadOrlov INT = (SELECT Id FROM Leaders WHERE FullName = N'Орлов Олег Олегович');

IF NOT EXISTS (SELECT 1 FROM Projects WHERE Customer = N'ОАО «Мосты и дороги»' AND ActivityId = @aRoads2)
BEGIN
    INSERT INTO Projects (ActivityId, Customer, Description, LeaderId, CreationDate, Status)
    VALUES (@aRoads2, N'ОАО «Мосты и дороги»', N'Проектирование участка автодороги длиной 12 км', @leadSidorov, DATEADD(DAY, -10, SYSUTCDATETIME()), N'активен');
END

IF NOT EXISTS (SELECT 1 FROM Projects WHERE Customer = N'ГК «РосАвтоДор»' AND ActivityId = @aIzysk2)
BEGIN
    INSERT INTO Projects (ActivityId, Customer, Description, LeaderId, CreationDate, Status)
    VALUES (@aIzysk2, N'ГК «РосАвтоДор»', N'Геологические и геодезические изыскания на трассе М-7', @leadSidorov, DATEADD(DAY, -5, SYSUTCDATETIME()), N'в очереди');
END

IF NOT EXISTS (SELECT 1 FROM Projects WHERE Customer = N'АО «ТрансМост»' AND ActivityId = @aBridgeInsp2)
BEGIN
    INSERT INTO Projects (ActivityId, Customer, Description, LeaderId, CreationDate, Status, CompletionDate)
    VALUES (@aBridgeInsp2, N'АО «ТрансМост»', N'Обследование двух пешеходных мостов', @leadOrlov, DATEADD(DAY, -30, SYSUTCDATETIME()), N'выполнен', DATEADD(DAY, -1, SYSUTCDATETIME()));
END
GO

-- Attach a worker to the active project
DECLARE @pActive INT = (SELECT TOP 1 Id FROM Projects WHERE Status = N'активен' ORDER BY CreationDate DESC);
IF @pActive IS NOT NULL
BEGIN
    DECLARE @w1 INT = (SELECT Id FROM Workers WHERE FullName = N'Кузнецов Алексей Викторович');
    DECLARE @w2 INT = (SELECT Id FROM Workers WHERE FullName = N'Соколов Дмитрий Сергеевич');
    IF @w1 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProjectWorkers WHERE ProjectId = @pActive AND WorkerId = @w1)
    BEGIN
        INSERT INTO ProjectWorkers (ProjectId, WorkerId) VALUES (@pActive, @w1);
        UPDATE Workers SET Status = N'занят' WHERE Id = @w1;
        INSERT INTO ProjectHistory (ProjectId, WorkerId, JoinedAt) VALUES (@pActive, @w1, SYSUTCDATETIME());
    END
    IF @w2 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProjectWorkers WHERE ProjectId = @pActive AND WorkerId = @w2)
    BEGIN
        INSERT INTO ProjectWorkers (ProjectId, WorkerId) VALUES (@pActive, @w2);
        UPDATE Workers SET Status = N'занят' WHERE Id = @w2;
        INSERT INTO ProjectHistory (ProjectId, WorkerId, JoinedAt) VALUES (@pActive, @w2, SYSUTCDATETIME());
    END
END
GO

-- Historical record for the completed project (worker history kept)
DECLARE @pDone INT = (SELECT TOP 1 Id FROM Projects WHERE Status = N'выполнен' ORDER BY CompletionDate DESC);
IF @pDone IS NOT NULL
BEGIN
    DECLARE @w3 INT = (SELECT Id FROM Workers WHERE FullName = N'Морозова Анна Игоревна');
    IF @w3 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ProjectHistory WHERE ProjectId = @pDone AND WorkerId = @w3)
    BEGIN
        DECLARE @joined DATETIME2 = DATEADD(DAY, -25, SYSUTCDATETIME());
        DECLARE @left DATETIME2 = DATEADD(DAY, -1, SYSUTCDATETIME());
        INSERT INTO ProjectHistory (ProjectId, WorkerId, JoinedAt, LeftAt) VALUES (@pDone, @w3, @joined, @left);
    END
END
GO
