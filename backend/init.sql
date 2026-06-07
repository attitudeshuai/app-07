CREATE TABLE IF NOT EXISTS Users (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Username VARCHAR(50) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    Role VARCHAR(20) NOT NULL DEFAULT 'Admin',
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS Products (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Description TEXT,
    PointsRequired INT NOT NULL,
    Stock INT NOT NULL DEFAULT 0,
    ImageUrl VARCHAR(500),
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS Orders (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    OrderNo VARCHAR(50) NOT NULL UNIQUE,
    ProductId INT NOT NULL,
    ProductName VARCHAR(100) NOT NULL,
    PointsConsumed INT NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    RecipientName VARCHAR(50) NOT NULL,
    RecipientPhone VARCHAR(20) NOT NULL,
    RecipientAddress VARCHAR(500) NOT NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'Pending',
    TrackingNumber VARCHAR(100),
    ShippingCompany VARCHAR(50),
    Remark TEXT,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (ProductId) REFERENCES Products(Id)
);

CREATE TABLE IF NOT EXISTS OrderHistories (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    OrderId INT NOT NULL,
    Status VARCHAR(20) NOT NULL,
    Remark TEXT,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (OrderId) REFERENCES Orders(Id)
);

CREATE TABLE IF NOT EXISTS MemberUsers (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Username VARCHAR(50) NOT NULL UNIQUE,
    Nickname VARCHAR(50),
    Phone VARCHAR(20) NOT NULL UNIQUE,
    Email VARCHAR(100),
    Avatar VARCHAR(500),
    Points INT NOT NULL DEFAULT 0,
    TotalPoints INT NOT NULL DEFAULT 0,
    Status VARCHAR(20) NOT NULL DEFAULT 'Active',
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS MemberLevels (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(50) NOT NULL UNIQUE,
    MinPoints INT NOT NULL DEFAULT 0,
    DiscountRate DECIMAL(5,2) NOT NULL DEFAULT 1.00,
    Description TEXT,
    SortOrder INT NOT NULL DEFAULT 0,
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS PointsRecords (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    MemberUserId INT NOT NULL,
    Type VARCHAR(20) NOT NULL,
    Points INT NOT NULL,
    Balance INT NOT NULL,
    Source VARCHAR(50) NOT NULL,
    Remark TEXT,
    OrderNo VARCHAR(50),
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (MemberUserId) REFERENCES MemberUsers(Id)
);

INSERT INTO Products (Name, Description, PointsRequired, Stock, ImageUrl)
SELECT '精美水杯', '高品质不锈钢保温杯，500ml容量', 500, 100, 'https://via.placeholder.com/300'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = '精美水杯');

INSERT INTO Products (Name, Description, PointsRequired, Stock, ImageUrl)
SELECT '商务笔记本', 'A5尺寸，优质纸张，100页', 300, 200, 'https://via.placeholder.com/300'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = '商务笔记本');

INSERT INTO Products (Name, Description, PointsRequired, Stock, ImageUrl)
SELECT '品牌雨伞', '全自动折叠伞，防紫外线', 800, 50, 'https://via.placeholder.com/300'
WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name = '品牌雨伞');

INSERT INTO MemberLevels (Name, MinPoints, DiscountRate, Description, SortOrder, IsActive)
SELECT '青铜', 0, 1.00, '初始会员等级，享受基础权益', 1, TRUE
WHERE NOT EXISTS (SELECT 1 FROM MemberLevels WHERE Name = '青铜');

INSERT INTO MemberLevels (Name, MinPoints, DiscountRate, Description, SortOrder, IsActive)
SELECT '白银', 1000, 0.95, '白银会员，享受95折优惠', 2, TRUE
WHERE NOT EXISTS (SELECT 1 FROM MemberLevels WHERE Name = '白银');

INSERT INTO MemberLevels (Name, MinPoints, DiscountRate, Description, SortOrder, IsActive)
SELECT '黄金', 5000, 0.90, '黄金会员，享受9折优惠', 3, TRUE
WHERE NOT EXISTS (SELECT 1 FROM MemberLevels WHERE Name = '黄金');

INSERT INTO MemberLevels (Name, MinPoints, DiscountRate, Description, SortOrder, IsActive)
SELECT '钻石', 10000, 0.80, '钻石会员，享受8折优惠', 4, TRUE
WHERE NOT EXISTS (SELECT 1 FROM MemberLevels WHERE Name = '钻石');
