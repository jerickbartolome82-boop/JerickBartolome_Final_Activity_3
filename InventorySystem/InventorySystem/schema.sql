-- Inventory Management System - database schema (run once)
CREATE DATABASE IF NOT EXISTS inventory_db
    CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE inventory_db;

CREATE TABLE IF NOT EXISTS users (
    user_id         INT AUTO_INCREMENT PRIMARY KEY,
    full_name       VARCHAR(100) NOT NULL,
    username        VARCHAR(50)  NOT NULL,
    email           VARCHAR(100) NOT NULL,
    password_hash   VARCHAR(255) NOT NULL,            -- "iterations.salt.hash" (PBKDF2-SHA256)
    role            ENUM('Administrator','Staff') NOT NULL DEFAULT 'Staff',
    is_active       TINYINT(1)   NOT NULL DEFAULT 1,
    failed_attempts INT          NOT NULL DEFAULT 0,
    locked_until    DATETIME     NULL DEFAULT NULL,
    last_login      DATETIME     NULL DEFAULT NULL,
    created_at      TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_users_username UNIQUE (username),
    CONSTRAINT uq_users_email    UNIQUE (email)
);

CREATE TABLE IF NOT EXISTS suppliers (
    supplier_id    INT AUTO_INCREMENT PRIMARY KEY,
    name           VARCHAR(100) NOT NULL,
    contact_person VARCHAR(100) NULL,
    phone          VARCHAR(20)  NULL,
    email          VARCHAR(100) NULL,
    address        VARCHAR(200) NULL,
    created_at     TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_suppliers_name UNIQUE (name)
);

CREATE TABLE IF NOT EXISTS products (
    product_id    INT AUTO_INCREMENT PRIMARY KEY,
    name          VARCHAR(100)  NOT NULL,
    category      VARCHAR(50)   NOT NULL,
    supplier_id   INT           NULL,                       -- deleting a supplier keeps the product
    quantity      INT           NOT NULL DEFAULT 0,         -- changed only through transactions
    reorder_level INT           NOT NULL DEFAULT 5,
    unit_price    DECIMAL(10,2) NOT NULL,
    created_at    TIMESTAMP     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_products_name UNIQUE (name),
    CONSTRAINT fk_products_supplier FOREIGN KEY (supplier_id)
        REFERENCES suppliers (supplier_id) ON DELETE SET NULL,
    CONSTRAINT chk_products_qty   CHECK (quantity >= 0),
    CONSTRAINT chk_products_price CHECK (unit_price >= 0)
);

CREATE TABLE IF NOT EXISTS transactions (
    transaction_id INT AUTO_INCREMENT PRIMARY KEY,
    product_id     INT          NOT NULL,
    type           ENUM('IN','OUT') NOT NULL,
    quantity       INT          NOT NULL,
    remarks        VARCHAR(200) NULL,
    performed_by   VARCHAR(50)  NOT NULL,
    created_at     TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_transactions_product FOREIGN KEY (product_id)
        REFERENCES products (product_id) ON DELETE CASCADE,  -- deleting a product removes its history
    CONSTRAINT chk_transactions_qty CHECK (quantity > 0),
    INDEX idx_transactions_created (created_at)
);
