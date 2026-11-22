PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Categories (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Name        NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(250) DEFAULT ''
);

CREATE TABLE IF NOT EXISTS Users (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    FullName     NVARCHAR(120) NOT NULL,
    Username     NVARCHAR(50)  NOT NULL UNIQUE,
    Email        NVARCHAR(120) DEFAULT '',
    Phone        NVARCHAR(20)  DEFAULT '',
    PasswordHash TEXT          NOT NULL,
    Role         NVARCHAR(20)  NOT NULL DEFAULT 'STAFF' CHECK (Role IN ('ADMIN', 'STAFF')),
    CreatedAt    DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS Products (
    Id         NVARCHAR(20)   PRIMARY KEY,
    Name       NVARCHAR(150)  NOT NULL,
    CategoryId INTEGER        NOT NULL,
    Price      DECIMAL(18, 2) NOT NULL CHECK (Price > 0),
    Quantity   INTEGER        NOT NULL CHECK (Quantity >= 0),
    UpdatedAt  DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId)
        REFERENCES Categories(Id) ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS InventoryLogs (
    Id                  INTEGER PRIMARY KEY AUTOINCREMENT,
    ProductId           NVARCHAR(20)  NULL,
    ProductNameSnapshot NVARCHAR(150) NOT NULL,
    UserId              INTEGER       NOT NULL,
    ActionType          NVARCHAR(30)  NOT NULL CHECK (ActionType IN ('IMPORT', 'EXPORT', 'CREATE', 'UPDATE', 'DELETE')),
    QuantityChange      INTEGER       NOT NULL,
    Note                NVARCHAR(300) DEFAULT '',
    CreatedAt           DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_InventoryLogs_Products FOREIGN KEY (ProductId)
        REFERENCES Products(Id) ON DELETE SET NULL,
    CONSTRAINT FK_InventoryLogs_Users FOREIGN KEY (UserId)
        REFERENCES Users(Id) ON DELETE RESTRICT
);

INSERT OR IGNORE INTO Categories (Id, Name, Description) VALUES
(1, N'Áo',       N'Áo thun, sơ mi, áo khoác thời trang'),
(2, N'Quần',     N'Quần Jeans, kaki, quần short nam nữ'),
(3, N'Giày dép', N'Giày sneaker, giày thể thao & dép'),
(4, N'Phụ kiện', N'Balo laptop, mũ lưỡi trai, túi xách');

INSERT OR IGNORE INTO Products (Id, Name, CategoryId, Price, Quantity) VALUES
('SP01', N'Áo Thun Polo Duke Classic',    1, 250000, 30),
('SP02', N'Áo Khoác Bomber Nam Đen',      1, 480000,  4),
('SP03', N'Quần Jeans Slimfit Xanh Đậm',  2, 390000, 15),
('SP04', N'Giày Sneaker Duke Trắng',      3, 650000,  0),
('SP05', N'Balo Laptop Chống Nước',       4, 320000, 12),
('SP06', N'Áo Sơ Mi Oxford Trắng',        1, 290000, 22),
('SP07', N'Quần Short Kaki Basic',        2, 210000,  3),
('SP08', N'Mũ Lưỡi Trai Thêu Logo Duke',  4, 150000, 18);
