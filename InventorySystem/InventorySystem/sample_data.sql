-- OPTIONAL demo data (6 suppliers, 25 products, stock history) so paging/search/reports have something to show.
-- Run once, after schema.sql.
USE inventory_db;

INSERT INTO suppliers (name, contact_person, phone, email, address) VALUES
('TechSource Trading',    'Maria Santos',   '0917-123-4567', 'sales@techsource.example',  'Quezon City, Metro Manila'),
('OfficeHub Supplies',    'Jose Reyes',     '0928-555-0101', 'orders@officehub.example',  'Makati City'),
('FurniWorks Inc.',       'Ana Cruz',       '(02) 8123-4567', 'info@furniworks.example',  'San Pablo City, Laguna'),
('Gadget Depot',          'Mark Villanueva','0905-777-8899', 'hello@gadgetdepot.example', 'Calamba, Laguna'),
('PrintPro Solutions',    'Liza Ramos',     '0939-222-3344', 'support@printpro.example',  'Pasig City'),
('ComfortSeats Co.',      'Ben Aquino',     '0918-444-1212', 'sales@comfortseats.example','Santa Rosa, Laguna');

INSERT INTO products (name, category, supplier_id, quantity, reorder_level, unit_price) VALUES
('Laptop 15 inch',          'Electronics',     1,   8,  5, 45000.00),
('Wireless Mouse',          'Accessories',     4,   3,  5,   375.00),
('Monitor 24 inch',         'Electronics',     1,   4,  5,  7500.00),
('Mechanical Keyboard',     'Accessories',     4,   0,  5,  2500.00),
('Office Chair',            'Furniture',       6,   7,  3,  3000.00),
('Laser Printer',           'Electronics',     5,   3,  4,  8000.00),
('Standing Desk',           'Furniture',       3,   0,  2, 12500.00),
('USB Flash Drive 64GB',    'Accessories',     4,  40, 10,   350.00),
('External Hard Drive 1TB', 'Electronics',     1,  12,  5,  3200.00),
('HDMI Cable 2m',           'Accessories',     4,  25, 10,   180.00),
('Webcam HD',               'Electronics',     1,   9,  5,  1450.00),
('Headset',                 'Accessories',     4,   6,  5,   990.00),
('Bond Paper A4 (Ream)',    'Office Supplies', 2, 120, 30,   245.00),
('Ballpen Blue (Box of 12)','Office Supplies', 2,  18, 20,    96.00),
('Stapler',                 'Office Supplies', 2,  15,  5,   150.00),
('Printer Ink Black',       'Office Supplies', 5,   4,  6,   650.00),
('Toner Cartridge',         'Office Supplies', 5,   0,  3,  3200.00),
('Filing Cabinet',          'Furniture',       3,   5,  3,  5800.00),
('Office Table',            'Furniture',       3,   8,  3,  4200.00),
('Whiteboard',              'Office Supplies', 2,  10,  4,  1200.00),
('WiFi Router',             'Electronics',     1,  14,  5,  1800.00),
('Power Strip',             'Accessories',     4,  30, 10,   320.00),
('Desk Lamp',               'Furniture',       6,  22,  8,   450.00),
('Calculator',              'Office Supplies', 2,   2,  5,   280.00),
('Laptop Bag',              'Accessories',     4,  16,  6,   850.00);

-- one "initial stock" history row per product that has stock
INSERT INTO transactions (product_id, type, quantity, remarks, performed_by)
SELECT product_id, 'IN', quantity, 'Initial stock', 'system' FROM products WHERE quantity > 0;
