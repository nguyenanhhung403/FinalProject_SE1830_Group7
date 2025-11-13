# Luồng Hoạt Động - Service Center & Inventory Management

## 📋 FLOW 1: SERVICE CENTER MANAGEMENT

### Mục đích
Quản lý các trung tâm dịch vụ, assign technicians vào service centers, và assign claims cho technicians.

---

### 1.1. Quản lý Service Centers (Admin/EVM Staff)

#### **Tạo Service Center mới**
```
User: Admin hoặc EVM Staff
Action: Vào menu "Service Centers" → Click "New Service Center"
Input:
  - Name: "Hanoi Service Center"
  - Address: "123 Main Street, Hanoi"
  - Contact Name: "Nguyen Van A"
  - Contact Phone: "0123456789"
Result: Service Center được tạo, hiển thị trong danh sách
```

#### **Xem danh sách Service Centers**
```
User: Admin, EVM Staff
Page: /ServiceCenters/Index
Hiển thị:
  - Danh sách tất cả service centers
  - Mỗi service center hiển thị:
    * Name, Address, Contact Info
    * Số lượng technicians đang assigned
    * Số lượng claims đang xử lý
    * Số lượng claims đã hoàn thành (tháng này)
```

#### **Xem chi tiết Service Center**
```
User: Admin, EVM Staff
Page: /ServiceCenters/Details?id=1
Hiển thị:
  - Thông tin chi tiết service center
  - Danh sách technicians đã assigned (với nút "Unassign")
  - Danh sách technicians chưa assigned (dropdown để assign)
  - Danh sách claims của service center này
  - Statistics: Total claims, Active claims, Completed this month
```

---

### 1.2. Assign Technicians to Service Centers

#### **Assign Technician**
```
User: Admin, EVM Staff
Location: ServiceCenters/Details page
Flow:
  1. Chọn technician từ dropdown "Available Technicians"
  2. Click button "Assign Technician"
  3. System kiểm tra:
     - Technician chưa được assign vào service center khác (IsActive = true)
     - Technician có role "SC Technician" hoặc "SC"
  4. Tạo record trong bảng ServiceCenterTechnicians:
     - ServiceCenterId = selected service center
     - UserId = selected technician
     - AssignedByUserId = current user
     - AssignedAt = now
     - IsActive = true
  5. SignalR notification gửi đến technician: "You have been assigned to [Service Center Name]"
  6. Technician xuất hiện trong danh sách "Assigned Technicians"
```

#### **Unassign Technician**
```
User: Admin, EVM Staff
Location: ServiceCenters/Details page
Flow:
  1. Click "Unassign" button next to technician name
  2. System update: IsActive = false (soft delete)
  3. Technician vẫn giữ lịch sử assignment nhưng không còn active
  4. SignalR notification: "You have been unassigned from [Service Center Name]"
```

---

### 1.3. Assign Claims to Technicians

#### **Assign Claim to Technician (từ Service Center Details)**
```
User: Admin, EVM Staff
Location: ServiceCenters/Details page → Claims section
Flow:
  1. Xem danh sách claims của service center
  2. Với mỗi claim chưa có technician (TechnicianId = null):
     - Dropdown hiển thị danh sách technicians đã assigned vào service center này
     - Chọn technician → Click "Assign"
  3. System update WarrantyClaim:
     - TechnicianId = selected technician
  4. SignalR notification:
     - Gửi đến technician: "New claim #123 assigned to you"
     - Gửi đến service center group: "Claim #123 assigned to [Technician Name]"
```

#### **Assign Claim to Technician (từ Claim Details)**
```
User: Admin, EVM Staff, SC Staff (của service center đó)
Location: Claims/Details page
Flow:
  1. Nếu claim.StatusCode = "Approved" và TechnicianId = null:
     - Hiển thị section "Assign Technician"
     - Dropdown hiển thị technicians của service center này
  2. Chọn technician → Click "Assign"
  3. System update WarrantyClaim.TechnicianId
  4. SignalR notification gửi đến technician
  5. Technician có thể thấy claim trong danh sách "My Assigned Claims"
```

---

### 1.4. Technician View

#### **Technician xem claims được assign**
```
User: SC Technician
Location: Claims/Index
Flow:
  1. Technician login → Vào Claims page
  2. System filter claims:
     - TechnicianId = current user
     - Status = "Approved" hoặc "InProgress"
  3. Technician thấy:
     - Claims đã được assign cho mình
     - Có thể "Start Repair" hoặc "Complete" claim
```

---

## 📦 FLOW 2: INVENTORY MANAGEMENT

### Mục đích
Quản lý stock levels của parts, track movements, và tự động reserve/release stock khi claims thay đổi.

---

### 2.1. Quản lý Stock Levels

#### **Xem Inventory Dashboard**
```
User: Admin, EVM Staff, SC Technician
Page: /Inventory/Index
Hiển thị:
  - Low Stock Alerts: Danh sách parts có StockQuantity < MinStockLevel
  - Total Inventory Value: Tổng giá trị inventory (sum of StockQuantity * UnitPrice)
  - Recent Stock Movements: 10 movements gần nhất
  - Charts:
    * Stock levels by part (bar chart)
    * Stock movements over time (line chart)
```

#### **Xem chi tiết Part với Stock Info**
```
User: Admin, EVM Staff, SC Technician
Page: /Parts/Details?id=1
Hiển thị:
  - Part information (PartCode, PartName, UnitPrice, WarrantyPeriodMonths)
  - Current Stock: 50 units
  - Min Stock Level: 10 units
  - Status: 
    * "In Stock" (nếu StockQuantity >= MinStockLevel)
    * "Low Stock" (nếu StockQuantity < MinStockLevel)
    * "Out of Stock" (nếu StockQuantity = 0)
  - Stock Movement History (table):
    * Date, Type (IN/OUT/RESERVED/RELEASED), Quantity, Reference (Claim #123), Note
```

---

### 2.2. Stock Movements - Tự động

#### **Auto Reserve Stock khi Add Part to Claim**
```
Trigger: Technician thêm part vào claim
Location: Claims/Details → Add Used Parts
Flow:
  1. Technician chọn part và quantity (ví dụ: Part A, Quantity = 2)
  2. Click "Add Part"
  3. System kiểm tra:
     - PartInventory.StockQuantity >= quantity requested
  4. Nếu đủ stock:
     - Tạo UsedPart record
     - Tạo PartStockMovement:
       * MovementType = "RESERVED"
       * Quantity = -2 (negative vì reserve)
       * ReferenceType = "CLAIM"
       * ReferenceId = claimId
       * Note = "Reserved for claim #123"
     - Update PartInventory:
       * StockQuantity = StockQuantity - 2
     - Success message: "Part added. Stock reserved."
  5. Nếu không đủ stock:
     - Error message: "Insufficient stock. Available: X units"
     - Không tạo UsedPart
```

#### **Auto Release Stock khi Claim Rejected**
```
Trigger: EVM Staff reject claim
Location: Claims/Details → Reject button
Flow:
  1. EVM Staff click "Reject"
  2. System tìm tất cả UsedParts của claim này
  3. Với mỗi UsedPart:
     - Tạo PartStockMovement:
       * MovementType = "RELEASED"
       * Quantity = +UsedPart.Quantity (positive vì release)
       * ReferenceType = "CLAIM"
       * ReferenceId = claimId
       * Note = "Released from rejected claim #123"
     - Update PartInventory:
       * StockQuantity = StockQuantity + UsedPart.Quantity
  4. UsedParts vẫn giữ trong database (để audit) nhưng stock đã được release
```

#### **Auto Consume Stock khi Claim Completed**
```
Trigger: Technician complete claim
Location: Claims/Details → Complete button
Flow:
  1. Technician click "Mark Completed"
  2. System tìm tất cả UsedParts của claim này
  3. Với mỗi UsedPart:
     - Tìm PartStockMovement có MovementType = "RESERVED" và ReferenceId = claimId
     - Tạo PartStockMovement mới:
       * MovementType = "OUT"
       * Quantity = -UsedPart.Quantity (negative vì consume)
       * ReferenceType = "CLAIM"
       * ReferenceId = claimId
       * Note = "Consumed for completed claim #123"
     - StockQuantity KHÔNG thay đổi (vì đã reserve trước đó, giờ chỉ mark là consumed)
  4. Stock đã được "consume" - không còn available
```

#### **Auto Release Stock khi Remove Part from Claim**
```
Trigger: Technician xóa part khỏi claim
Location: Claims/Details → Delete Used Part
Flow:
  1. Technician click "Delete" trên UsedPart
  2. System tìm PartStockMovement có MovementType = "RESERVED" và ReferenceId = claimId và PartId = partId
  3. Tạo PartStockMovement:
     * MovementType = "RELEASED"
     * Quantity = +UsedPart.Quantity
     * ReferenceType = "CLAIM"
     * ReferenceId = claimId
     * Note = "Released from claim #123 (part removed)"
  4. Update PartInventory:
     * StockQuantity = StockQuantity + UsedPart.Quantity
  5. Xóa UsedPart record
```

---

### 2.3. Stock Movements - Thủ công

#### **Manual Stock Adjustment**
```
User: Admin, EVM Staff
Page: /Inventory/AdjustStock?partId=1
Flow:
  1. Chọn Part từ dropdown
  2. Nhập Adjustment Type:
     - "IN" (nhập kho): Quantity = positive (ví dụ: +10)
     - "OUT" (xuất kho): Quantity = negative (ví dụ: -5)
     - "ADJUSTMENT" (điều chỉnh): Quantity có thể positive hoặc negative
  3. Nhập Reason: "Stock count correction", "Damaged items", etc.
  4. Click "Adjust Stock"
  5. System:
     - Tạo PartStockMovement:
       * MovementType = selected type
       * Quantity = entered quantity
       * ReferenceType = "ADJUSTMENT"
       * ReferenceId = null
       * Note = reason
       * CreatedByUserId = current user
     - Update PartInventory:
       * StockQuantity = StockQuantity + quantity
  6. Success message: "Stock adjusted. New quantity: X units"
```

#### **Set Min Stock Level**
```
User: Admin, EVM Staff
Page: /Parts/Details?id=1
Flow:
  1. Trong Part Details page, có field "Min Stock Level"
  2. Nhập giá trị (ví dụ: 10)
  3. Click "Update Min Stock Level"
  4. System update PartInventory.MinStockLevel
  5. Nếu StockQuantity < MinStockLevel:
     - Part xuất hiện trong "Low Stock Alerts" dashboard
     - SignalR notification gửi đến Admin/EVM Staff: "Part [PartName] is low on stock"
```

---

### 2.4. Low Stock Alerts

#### **Real-time Low Stock Notification**
```
Trigger: Khi StockQuantity < MinStockLevel (sau bất kỳ movement nào)
Flow:
  1. System kiểm tra sau mỗi stock movement
  2. Nếu StockQuantity < MinStockLevel:
     - Tạo notification (nếu chưa có alert cho part này)
     - SignalR broadcast đến Admin và EVM Staff groups:
       * Message: "⚠️ Low Stock Alert: [PartName] - Only X units remaining (Min: Y)"
       * Link: /Parts/Details?id=[PartId]
  3. Alert tự động clear khi StockQuantity >= MinStockLevel
```

---

## 🔄 INTEGRATION FLOW - Complete Example

### Scenario: Complete Claim với Inventory Tracking

```
1. SC Staff tạo claim #100 cho vehicle VIN123
   → Status: "Pending"
   → No parts yet

2. EVM Staff approve claim #100
   → Status: "Approved"
   → TechnicianId: null (chưa assign)

3. Admin assign technician "John" (SC Technician) vào claim #100
   → WarrantyClaim.TechnicianId = John's UserId
   → SignalR: John nhận notification "Claim #100 assigned to you"

4. John (Technician) vào Claims/Details?id=100
   → Click "Start Repair"
   → Status: "InProgress"

5. John thêm Part A (Quantity = 3) vào claim
   → System check: PartInventory.StockQuantity = 50, MinStockLevel = 10
   → StockQuantity >= 3? YES
   → Create UsedPart record
   → Create PartStockMovement: RESERVED, Quantity = -3, ReferenceId = 100
   → Update PartInventory: StockQuantity = 50 - 3 = 47
   → Success: "Part A added. 3 units reserved."

6. John thêm Part B (Quantity = 5) vào claim
   → System check: PartInventory.StockQuantity = 8, MinStockLevel = 10
   → StockQuantity >= 5? YES (8 >= 5)
   → Create UsedPart record
   → Create PartStockMovement: RESERVED, Quantity = -5, ReferenceId = 100
   → Update PartInventory: StockQuantity = 8 - 5 = 3
   → Warning: "Part B added. Low stock alert! Only 3 units remaining (Min: 10)"
   → SignalR: Admin/EVM Staff nhận low stock alert

7. John complete claim #100
   → Status: "Completed"
   → System tìm UsedParts của claim #100:
     * Part A: 3 units (RESERVED)
     * Part B: 5 units (RESERVED)
   → Create PartStockMovements: OUT, Quantity = -3 và -5, ReferenceId = 100
   → StockQuantity KHÔNG thay đổi (đã reserve rồi, giờ consume)
   → TotalCost = sum of (UsedPart.Quantity * UsedPart.PartCost)

8. Admin archive claim #100
   → Status: "Archived"
   → Claim moved to WarrantyHistory
```

---

## 📊 USER ROLES & PERMISSIONS

### Service Center Management
- **Admin**: Full access (CRUD service centers, assign/unassign technicians, assign claims)
- **EVM Staff**: View service centers, assign technicians, assign claims
- **SC Staff**: View own service center, view assigned technicians
- **SC Technician**: View own assignments, view claims assigned to them

### Inventory Management
- **Admin**: Full access (view inventory, adjust stock, set min levels)
- **EVM Staff**: View inventory, adjust stock, set min levels, receive low stock alerts
- **SC Technician**: View inventory, view stock levels, receive low stock alerts
- **SC Staff**: View inventory (read-only)

---

## 🔔 SIGNALR NOTIFICATIONS

### Service Center Notifications
- Technician assigned to service center
- Technician unassigned from service center
- Claim assigned to technician
- New claim created for service center

### Inventory Notifications
- Low stock alert (when StockQuantity < MinStockLevel)
- Stock adjusted (manual adjustment)
- Stock reserved (when part added to claim)
- Stock released (when claim rejected or part removed)

