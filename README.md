# 积分商城后端 API 服务

一个基于 .NET 7.0 + MySQL 的积分商城纯后端服务，提供完整的 RESTful API 接口，支持商品管理、订单管理、会员管理、积分管理等核心业务功能。

## 功能特性

### 用户认证
- JWT + BCrypt 安全认证机制
- 管理员账号管理
- Token 有效期配置

### 商品管理
- 商品列表查询（支持搜索、分页、筛选）
- 商品详情查询
- 新增、编辑、删除商品
- 商品上下架管理
- 库存调整

### 订单管理
- 订单列表查询（支持状态筛选、搜索、分页）
- 订单详情查询（含历史记录）
- 创建订单
- 订单发货（物流信息）
- 订单完成确认
- 退换货处理
- 订单取消（自动恢复库存）

### 会员管理
- 会员列表查询（支持搜索、状态筛选、分页）
- 会员详情查询
- 新增、编辑、删除会员
- 积分调整（增减）
- 会员状态管理

### 积分记录
- 积分记录列表查询（多条件筛选）
- 积分记录详情
- 积分统计汇总

### 数据概览
- 商品、订单、会员总数统计
- 各状态订单数量统计
- 库存预警提醒
- 最近订单列表
- 订单趋势统计

## 技术栈

- **框架**: .NET 7.0 (ASP.NET Core)
- **ORM**: Entity Framework Core
- **数据库**: MySQL 8.0
- **认证**: JWT Bearer
- **密码加密**: BCrypt.Net-Next
- **API 文档**: Swagger / OpenAPI
- **部署**: Docker & Docker Compose

## 快速开始

### 环境要求
- Docker 20.10+
- Docker Compose 2.0+

### 一键启动

```bash
# 进入项目根目录
cd app-07

# 一键启动所有服务
docker-compose up --build
```

等待服务启动完成后：
- **后端 API 地址**: http://localhost:5000
- **Swagger API 文档**: http://localhost:5000

### 默认管理员账号

系统启动时会**自动初始化**默认管理员账号：

- 用户名: `admin`
- 密码: `123456`

> 提示：如果登录失败，可调用初始化接口 `POST /api/auth/init-admin`。

### 本地开发

```bash
cd backend

# 还原依赖
dotnet restore

# 运行项目
dotnet run
```

## 项目结构

```
app-07/
├── backend/                    # .NET 7.0 后端项目
│   ├── Controllers/           # API 控制器
│   │   ├── AuthController.cs         # 认证接口
│   │   ├── ProductsController.cs     # 商品管理接口
│   │   ├── OrdersController.cs       # 订单管理接口
│   │   ├── MemberUsersController.cs  # 会员管理接口
│   │   ├── PointsRecordsController.cs # 积分记录接口
│   │   └── DashboardController.cs    # 数据概览接口
│   ├── Data/                  # 数据库上下文
│   │   └── ApplicationDbContext.cs
│   ├── Dtos/                  # 数据传输对象
│   │   ├── AuthDtos.cs
│   │   ├── ProductDtos.cs
│   │   ├── OrderDtos.cs
│   │   ├── MemberUserDtos.cs
│   │   └── PointsRecordDtos.cs
│   ├── Models/                # 实体模型
│   │   ├── User.cs
│   │   ├── Product.cs
│   │   ├── Order.cs
│   │   ├── MemberUser.cs
│   │   └── PointsRecord.cs
│   ├── init.sql               # 数据库初始化脚本
│   ├── appsettings.json       # 应用配置
│   ├── Dockerfile             # Docker 构建文件
│   └── PointsMall.csproj      # 项目文件
├── docker-compose.yml          # Docker Compose 配置
├── .gitignore
└── README.md
```

## API 接口文档

所有接口统一返回格式：

```json
{
  "success": true,
  "message": "操作成功",
  "data": { ... }
}
```

### 认证接口

| 方法 | 路径 | 说明 | 权限 |
|------|------|------|------|
| POST | `/api/auth/login` | 用户登录 | 公开 |
| POST | `/api/auth/init-admin` | 初始化管理员账号 | 公开 |

#### 登录示例

**请求**:
```bash
POST /api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "123456"
}
```

**响应**:
```json
{
  "success": true,
  "message": "登录成功",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "username": "admin",
    "role": "Admin",
    "expiresAt": "2024-01-02T00:00:00Z"
  }
}
```

### 商品接口

| 方法 | 路径 | 说明 | 权限 |
|------|------|------|------|
| GET | `/api/products` | 获取商品列表 | 公开 |
| GET | `/api/products/{id}` | 获取商品详情 | 公开 |
| POST | `/api/products` | 创建商品 | 需要认证 |
| PUT | `/api/products/{id}` | 更新商品 | 需要认证 |
| PUT | `/api/products/{id}/stock` | 更新库存 | 需要认证 |
| DELETE | `/api/products/{id}` | 删除商品 | 需要认证 |

#### 获取商品列表

**请求参数**:
- `search` (可选): 搜索关键词（名称/描述）
- `isActive` (可选): 是否上架
- `page`: 页码，默认 1
- `pageSize`: 每页数量，默认 20

**示例**:
```bash
GET /api/products?search=水杯&isActive=true&page=1&pageSize=10
Authorization: Bearer {token}
```

### 订单接口

| 方法 | 路径 | 说明 | 权限 |
|------|------|------|------|
| GET | `/api/orders` | 获取订单列表 | 需要认证 |
| GET | `/api/orders/{id}` | 获取订单详情 | 需要认证 |
| POST | `/api/orders` | 创建订单 | 公开 |
| PUT | `/api/orders/{id}/ship` | 订单发货 | 需要认证 |
| PUT | `/api/orders/{id}/complete` | 完成订单 | 需要认证 |
| PUT | `/api/orders/{id}/return` | 退换货处理 | 需要认证 |
| PUT | `/api/orders/{id}/cancel` | 取消订单 | 需要认证 |

#### 订单状态说明

| 状态 | 说明 | 可执行操作 |
|------|------|------------|
| Pending | 待发货 | 发货、取消 |
| Shipped | 已发货 | 完成、退换货 |
| Completed | 已完成 | 退换货 |
| Returned | 退换货 | - |
| Cancelled | 已取消 | - |

### 会员接口

| 方法 | 路径 | 说明 | 权限 |
|------|------|------|------|
| GET | `/api/memberusers` | 获取会员列表 | 需要认证 |
| GET | `/api/memberusers/{id}` | 获取会员详情 | 需要认证 |
| POST | `/api/memberusers` | 创建会员 | 需要认证 |
| PUT | `/api/memberusers/{id}` | 更新会员信息 | 需要认证 |
| DELETE | `/api/memberusers/{id}` | 删除会员 | 需要认证 |
| PUT | `/api/memberusers/{id}/adjust-points` | 调整积分 | 需要认证 |

### 积分记录接口

| 方法 | 路径 | 说明 | 权限 |
|------|------|------|------|
| GET | `/api/pointsrecords` | 获取积分记录列表 | 需要认证 |
| GET | `/api/pointsrecords/{id}` | 获取积分记录详情 | 需要认证 |
| GET | `/api/pointsrecords/summary` | 积分统计汇总 | 需要认证 |

### 数据概览接口

| 方法 | 路径 | 说明 | 权限 |
|------|------|------|------|
| GET | `/api/dashboard/stats` | 获取统计数据 | 需要认证 |
| GET | `/api/dashboard/order-trend` | 获取订单趋势 | 需要认证 |

## 配置说明

### 数据库配置

在 `docker-compose.yml` 中配置：

```yaml
environment:
  - ConnectionStrings__DefaultConnection=Server=mysql;Port=3306;Database=PointsMallDB;User=pointsmall;Password=pointsmall123;
```

### JWT 配置

在 `docker-compose.yml` 中配置：

```yaml
environment:
  - Jwt__Key=ThisIsASecretKeyForJwtAuthenticationPointsMall2024
  - Jwt__Issuer=PointsMall
  - Jwt__Audience=PointsMallUsers
  - Jwt__ExpiresInMinutes=1440
```

> 注意：生产环境请务必修改 JWT 密钥！

## 部署说明

### Docker Compose 部署（推荐）

```bash
# 启动服务
docker-compose up -d

# 查看日志
docker-compose logs -f backend

# 停止服务
docker-compose down

# 停止并删除数据卷（慎用！会清除数据库数据）
docker-compose down -v
```

### 独立部署

1. 安装 .NET 7.0 Runtime
2. 安装 MySQL 8.0
3. 发布项目：
   ```bash
   cd backend
   dotnet publish -c Release -o ./publish
   ```
4. 配置 `appsettings.json` 中的数据库连接字符串
5. 运行：
   ```bash
   cd publish
   dotnet PointsMall.dll
   ```

## 常见问题

### 1. 数据库连接失败？
确保 MySQL 服务已启动，连接字符串配置正确。使用 Docker Compose 时，MySQL 首次启动可能需要 30-60 秒进行初始化。

### 2. 登录失败？
请确保已调用 `/api/auth/init-admin` 接口初始化管理员账号，或检查数据库中是否存在 admin 用户。

### 3. 如何修改 JWT 密钥？
编辑 `docker-compose.yml` 中后端服务的环境变量 `Jwt__Key`，重启服务即可生效。

### 4. Swagger 文档无法访问？
本项目在生产环境也启用了 Swagger，直接访问根路径 `http://localhost:5000` 即可查看 API 文档。

## 许可证

MIT License
