# N8N AI Suggest Prompt Template

## ⚠️ QUAN TRỌNG: Xử lý ảnh cho AI

**Vấn đề:** AI model (OpenAI, Claude, etc.) không thể truy cập trực tiếp URL nội bộ như `http://host.docker.internal:5125/uploads/...`

**Giải pháp:** Cần tải ảnh về trong n8n và gửi dưới dạng **base64** cho AI model.

---

## Cấu trúc n8n Workflow (ĐỀ XUẤT):

1. **Webhook** - Nhận dữ liệu từ ASP.NET
2. **IF Node** - Kiểm tra có `imageUrl` không
3. **HTTP Request (Get Image)** - Tải ảnh từ URL (chỉ chạy nếu có imageUrl)
4. **Function (Convert to Base64)** - Convert ảnh binary thành base64
5. **Function** - Tạo prompt với ảnh base64
6. **OpenAI Chat** hoặc **AI Agent** - Gửi prompt + ảnh base64 tới AI
7. **Function** - Format response (tùy chọn)
8. **Respond to Webhook** - Trả kết quả về ASP.NET

---

## Chi tiết từng Node:

### 1. Webhook Node
- **Method:** POST
- **Path:** `/webhook-test/ai-suggest` (hoặc path bạn đã cấu hình)
- Nhận dữ liệu từ ASP.NET với `imageUrl` trong body

### 2. IF Node (Kiểm tra có ảnh)
**Condition:**
```javascript
{{ $json.body.imageUrl && $json.body.imageUrl !== '' }}
```

### 3. HTTP Request Node (Get Image from URL)
**Cấu hình:**
- **Method:** GET
- **URL:** `{{ $json.body.imageUrl }}`
- **Authentication:** None
- **Options:**
  - ✅ **Ignore SSL Issues** (nếu dùng HTTPS nội bộ)
  - **Response Format:** File (Binary)

**Output:** Node này sẽ trả về binary data của ảnh

### 4. Function Node (Convert Image to Base64)
**Code:**
```javascript
// Lấy binary data từ HTTP Request node
const binaryData = $input.item.binary.data;

// Convert sang base64
const base64Image = binaryData.data.toString('base64');

// Lấy MIME type từ binary data
const mimeType = binaryData.mimeType || 'image/jpeg';

// Tạo data URL
const dataUrl = `data:${mimeType};base64,${base64Image}`;

return [{
  json: {
    ...$json.body, // Giữ nguyên tất cả dữ liệu từ webhook
    imageBase64: base64Image,
    imageDataUrl: dataUrl,
    imageMimeType: mimeType
  }
}];
```

### 5. Function Node (Tạo Prompt với ảnh)
**Code:**
```javascript
const claim = $json.body || $json; // Lấy từ webhook hoặc từ node trước

const formatDate = (dateStr) => {
  if (!dateStr) return 'N/A';
  try {
    return new Date(dateStr).toLocaleDateString('vi-VN');
  } catch {
    return dateStr;
  }
};

const prompt = `Bạn là một chuyên gia kỹ thuật sửa chữa xe điện (EV) với nhiều năm kinh nghiệm. Nhiệm vụ của bạn là phân tích yêu cầu bảo hành và đưa ra gợi ý chi tiết về chẩn đoán và sửa chữa.

## THÔNG TIN CƠ BẢN

**Mã Claim:** #${claim.claimId}
**Trạng thái:** ${claim.status}
**Ngày phát hiện:** ${formatDate(claim.dateDiscovered)}
**Ngày tạo:** ${formatDate(claim.createdAt)}

## THÔNG TIN XE

**Dòng xe:** ${claim.vehicleModel || 'N/A'}
**VIN:** ${claim.vin || 'N/A'}
**Năm sản xuất:** ${claim.vehicleYear || 'N/A'}

## KHÁCH HÀNG

${claim.customer ? 
  `- **Tên:** ${claim.customer.fullName || 'N/A'}
- **Email:** ${claim.customer.email || 'N/A'}
- **Điện thoại:** ${claim.customer.phone || 'N/A'}` : 
  '- Không có thông tin khách hàng'}

## TRUNG TÂM DỊCH VỤ

**Tên:** ${claim.serviceCenterName || 'N/A'}
**Địa chỉ:** ${claim.serviceCenterAddress || 'N/A'}

## NGƯỜI XỬ LÝ

**Người tạo:** ${claim.requestedByUserName || 'N/A'}
${claim.assignedTechnician ? 
  `**Kỹ thuật viên được phân công:** ${claim.assignedTechnician.fullName || 'N/A'} (${claim.assignedTechnician.email || 'N/A'})` : 
  '**Kỹ thuật viên:** Chưa phân công'}

## MÔ TẢ VẤN ĐỀ

${claim.description || 'Không có mô tả'}

${claim.note ? `\n**Ghi chú kỹ thuật:** ${claim.note}` : ''}

${claim.usedParts && claim.usedParts.length > 0 ? `
## LINH KIỆN ĐÃ SỬ DỤNG
${claim.usedParts.map(p => `- ${p.partCode || 'N/A'} - ${p.partName || 'N/A'}: Số lượng ${p.quantity}, Giá ${p.unitCost || 0} VNĐ/đơn vị`).join('\n')}
` : ''}

${claim.availableParts && claim.availableParts.length > 0 ? `
## DANH SÁCH LINH KIỆN CÓ SẴN TRONG CỬA HÀNG
${claim.availableParts.map(p => `- Mã: ${p.partCode || 'N/A'} | Tên: ${p.partName || 'N/A'} | Giá: ${p.unitPrice || 0} VNĐ${p.warrantyPeriodMonths ? ' | Bảo hành: ' + p.warrantyPeriodMonths + ' tháng' : ''}`).join('\n')}
` : ''}

## YÊU CẦU PHÂN TÍCH

Hãy đưa ra gợi ý chi tiết về:

1. **CHẨN ĐOÁN VẤN ĐỀ:**
   - Nguyên nhân có thể gây ra vấn đề
   - Bộ phận có thể bị hỏng
   - Mức độ nghiêm trọng

2. **LINH KIỆN ĐỀ XUẤT:**
   - Dựa vào danh sách linh kiện có sẵn trong cửa hàng ở trên, đề xuất các linh kiện phù hợp để sửa chữa
   - Chỉ đề xuất các linh kiện có trong danh sách có sẵn
   - Ghi rõ mã linh kiện (PartCode), tên, số lượng cần thiết, và giá
   - Đảm bảo tương thích với dòng xe ${claim.vehicleModel || 'này'}

3. **QUY TRÌNH SỬA CHỮA:**
   - Các bước thực hiện theo thứ tự
   - Công cụ và thiết bị cần thiết
   - Thời gian ước tính

4. **LƯU Ý KỸ THUẬT:**
   - Cảnh báo an toàn
   - Điểm cần chú ý đặc biệt
   - Khuyến nghị bảo dưỡng sau sửa chữa

5. **ƯỚC TÍNH CHI PHÍ:**
   - Chi phí linh kiện (dựa trên giá trong danh sách có sẵn)
   - Chi phí nhân công ước tính
   - Tổng chi phí dự kiến

${claim.imageBase64 ? '\n**Lưu ý:** Có ảnh minh chứng được đính kèm - Hãy phân tích kỹ hình ảnh để đưa ra chẩn đoán chính xác hơn.' : ''}

Hãy trả lời bằng tiếng Việt, rõ ràng và chi tiết.`;

return [{
  json: {
    prompt: prompt,
    imageBase64: claim.imageBase64,
    imageMimeType: claim.imageMimeType,
    claimId: claim.claimId,
    vehicleModel: claim.vehicleModel,
    description: claim.description
  }
}];
```

### 6. OpenAI Chat Node (hoặc AI Agent Node)

#### Nếu dùng **OpenAI Chat Node:**

**System Message:**
```
Bạn là một chuyên gia kỹ thuật sửa chữa xe điện (EV) với nhiều năm kinh nghiệm. Nhiệm vụ của bạn là phân tích yêu cầu bảo hành và đưa ra gợi ý chi tiết về chẩn đoán và sửa chữa. Hãy trả lời bằng tiếng Việt, rõ ràng và chi tiết.
```

**User Message:**
```
{{ $json.prompt }}
```

**Attachments (nếu có ảnh):**
- **Type:** Image
- **Data:** `{{ $json.imageBase64 }}`
- **MIME Type:** `{{ $json.imageMimeType || 'image/jpeg' }}`

**Hoặc dùng Content Array format:**
```javascript
// Trong Function node trước OpenAI, tạo content array:
const content = [
  {
    type: "text",
    text: $json.prompt
  }
];

if ($json.imageBase64) {
  content.push({
    type: "image_url",
    image_url: {
      url: `data:${$json.imageMimeType || 'image/jpeg'};base64,${$json.imageBase64}`
    }
  });
}

return [{ json: { messages: [{ role: "user", content: content }] } }];
```

#### Nếu dùng **AI Agent Node (n8n):**

**Prompt (User Message):**
```
{{ $json.prompt }}
```

**Attachments:**
- Thêm attachment với type "Image"
- Data: `{{ $json.imageBase64 }}`
- MIME Type: `{{ $json.imageMimeType || 'image/jpeg' }}`

**Lưu ý:** Một số AI Agent node có thể yêu cầu format khác, hãy kiểm tra documentation của node đó.

### 7. Function Node (Format Response) - Tùy chọn
```javascript
// Lấy response từ AI
const aiResponse = $json.output || $json.message || $json.text || $json.content;

// Format lại nếu cần
return [{
  json: {
    output: aiResponse,
    claimId: $json.claimId || $('Webhook').item.json.body.claimId
  }
}];
```

### 8. Respond to Webhook Node
**Response Data:**
```javascript
{
  "output": "{{ $json.output || $json.message || $json.text }}"
}
```

**Hoặc nếu muốn format đầy đủ hơn:**
```javascript
{
  "success": true,
  "output": "{{ $json.output || $json.message || $json.text || $json.content }}",
  "claimId": {{ $json.claimId || $('Webhook').item.json.body.claimId }}
}
```

---

## Workflow Diagram (Text):

```
[Webhook] 
    ↓
[IF: Has imageUrl?]
    ↓ YES
[HTTP Request: Get Image]
    ↓
[Function: Convert to Base64]
    ↓
[Function: Create Prompt]
    ↓
[OpenAI Chat / AI Agent] ← (Nhận prompt + base64 image)
    ↓
[Function: Format Response]
    ↓
[Respond to Webhook]
```

**Nếu không có ảnh:**
```
[Webhook]
    ↓
[IF: Has imageUrl?]
    ↓ NO
[Function: Create Prompt (no image)]
    ↓
[OpenAI Chat / AI Agent] ← (Chỉ nhận prompt)
    ↓
[Function: Format Response]
    ↓
[Respond to Webhook]
```

---

## Lưu ý quan trọng:

1. **Base64 Size Limit:**
   - OpenAI Vision API: Tối đa 20MB cho ảnh
   - Nếu ảnh quá lớn, có thể cần resize trước khi convert base64

2. **MIME Type:**
   - Đảm bảo MIME type đúng (image/jpeg, image/png, etc.)
   - Lấy từ binary data hoặc detect từ file extension

3. **Error Handling:**
   - Thêm Try-Catch trong Function nodes
   - Xử lý trường hợp không tải được ảnh
   - Fallback về prompt không có ảnh nếu lỗi

4. **Testing:**
   - Test với ảnh nhỏ trước
   - Kiểm tra base64 encoding đúng format
   - Verify AI nhận được ảnh và phân tích được

5. **Performance:**
   - Ảnh base64 sẽ làm tăng kích thước payload
   - Cân nhắc cache ảnh đã convert nếu cần

---

## Ví dụ Function Node hoàn chỉnh (Merge tất cả):

Nếu muốn gộp tất cả vào 1 Function node sau HTTP Request:

```javascript
// Lấy binary data từ HTTP Request
const binaryData = $input.item.binary.data;
const base64Image = binaryData.data.toString('base64');
const mimeType = binaryData.mimeType || 'image/jpeg';

// Lấy claim data từ webhook
const claim = $('Webhook').item.json.body;

// Format date helper
const formatDate = (dateStr) => {
  if (!dateStr) return 'N/A';
  try {
    return new Date(dateStr).toLocaleDateString('vi-VN');
  } catch {
    return dateStr;
  }
};

// Tạo prompt
const prompt = `Bạn là một chuyên gia kỹ thuật sửa chữa xe điện (EV)...

[Prompt content như trên, nhưng không cần check imageUrl nữa vì đã có base64]

**Lưu ý:** Có ảnh minh chứng được đính kèm - Hãy phân tích kỹ hình ảnh để đưa ra chẩn đoán chính xác hơn.`;

// Tạo content array cho OpenAI
const content = [
  {
    type: "text",
    text: prompt
  },
  {
    type: "image_url",
    image_url: {
      url: `data:${mimeType};base64,${base64Image}`
    }
  }
];

return [{
  json: {
    messages: [{
      role: "user",
      content: content
    }],
    claimId: claim.claimId
  }
}];
```

---

## Troubleshooting:

**Vấn đề:** AI không nhận được ảnh
- ✅ Kiểm tra base64 encoding đúng format
- ✅ Kiểm tra MIME type đúng
- ✅ Kiểm tra data URL format: `data:image/jpeg;base64,{base64string}`
- ✅ Kiểm tra size ảnh không vượt quá limit

**Vấn đề:** HTTP Request không tải được ảnh
- ✅ Kiểm tra URL đúng format
- ✅ Kiểm tra network connectivity từ n8n container
- ✅ Kiểm tra `host.docker.internal` hoạt động đúng
- ✅ Enable "Ignore SSL Issues" nếu dùng HTTPS nội bộ

**Vấn đề:** Base64 quá lớn
- ✅ Resize ảnh trước khi convert
- ✅ Compress ảnh nếu cần
- ✅ Hoặc chỉ gửi URL và để AI tự fetch (nếu AI hỗ trợ public URL)
