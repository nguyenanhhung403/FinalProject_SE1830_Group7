# N8N AI Suggest Prompt Template

## Sử dụng trong n8n Function Node hoặc OpenAI Chat Node

### Prompt Template (cho Function Node):

```javascript
// Trong n8n Function node, sử dụng code này để tạo prompt
const claim = $json.body;

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

${claim.imageUrl ? `\n**Ảnh minh chứng:** ${claim.imageUrl}` : ''}

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

${claim.imageUrl ? '\n**Lưu ý:** Có ảnh minh chứng - Hãy phân tích kỹ hình ảnh để đưa ra chẩn đoán chính xác hơn.' : ''}

Hãy trả lời bằng tiếng Việt, rõ ràng và chi tiết.`;

return [{
  json: {
    prompt: prompt,
    claimId: claim.claimId,
    vehicleModel: claim.vehicleModel,
    description: claim.description
  }
}];
```

---

## Sử dụng trong OpenAI Chat Node:

### System Message:
```
Bạn là một chuyên gia kỹ thuật sửa chữa xe điện (EV) với nhiều năm kinh nghiệm. Nhiệm vụ của bạn là phân tích yêu cầu bảo hành và đưa ra gợi ý chi tiết về chẩn đoán và sửa chữa. Hãy trả lời bằng tiếng Việt, rõ ràng và chi tiết.
```

### User Message (dùng Expression):
```
Phân tích yêu cầu bảo hành sau:

**THÔNG TIN CƠ BẢN:**
- Mã Claim: #{{ $json.body.claimId }}
- Trạng thái: {{ $json.body.status }}
- Ngày phát hiện: {{ $json.body.dateDiscovered ? new Date($json.body.dateDiscovered).toLocaleDateString('vi-VN') : 'N/A' }}
- Ngày tạo: {{ $json.body.createdAt ? new Date($json.body.createdAt).toLocaleDateString('vi-VN') : 'N/A' }}

**THÔNG TIN XE:**
- Dòng xe: {{ $json.body.vehicleModel || 'N/A' }}
- VIN: {{ $json.body.vin || 'N/A' }}
- Năm sản xuất: {{ $json.body.vehicleYear || 'N/A' }}

**KHÁCH HÀNG:**
{{ $json.body.customer ? '- Tên: ' + $json.body.customer.fullName + '\n- Email: ' + ($json.body.customer.email || 'N/A') + '\n- Điện thoại: ' + ($json.body.customer.phone || 'N/A') : '- Không có thông tin khách hàng' }}

**TRUNG TÂM DỊCH VỤ:**
- Tên: {{ $json.body.serviceCenterName || 'N/A' }}
- Địa chỉ: {{ $json.body.serviceCenterAddress || 'N/A' }}

**NGƯỜI XỬ LÝ:**
- Người tạo: {{ $json.body.requestedByUserName || 'N/A' }}
{{ $json.body.assignedTechnician ? '- Kỹ thuật viên được phân công: ' + $json.body.assignedTechnician.fullName + ' (' + ($json.body.assignedTechnician.email || 'N/A') + ')' : '- Chưa phân công kỹ thuật viên' }}

**MÔ TẢ VẤN ĐỀ:**
{{ $json.body.description || 'Không có mô tả' }}

{{ $json.body.note ? '**Ghi chú kỹ thuật:** ' + $json.body.note + '\n' : '' }}

{{ $json.body.imageUrl ? '**Ảnh minh chứng:** ' + $json.body.imageUrl + '\n' : '' }}

{{ $json.body.usedParts && $json.body.usedParts.length > 0 ? '**Linh kiện đã sử dụng:**\n' + $json.body.usedParts.map(p => '- ' + (p.partCode || 'N/A') + ' - ' + (p.partName || 'N/A') + ': Số lượng ' + p.quantity + ', Giá ' + (p.unitCost || 0) + ' VNĐ/đơn vị').join('\n') + '\n' : '' }}

{{ $json.body.availableParts && $json.body.availableParts.length > 0 ? '**DANH SÁCH LINH KIỆN CÓ SẴN TRONG CỬA HÀNG:**\n' + $json.body.availableParts.map(p => '- Mã: ' + (p.partCode || 'N/A') + ' | Tên: ' + (p.partName || 'N/A') + ' | Giá: ' + (p.unitPrice || 0) + ' VNĐ' + (p.warrantyPeriodMonths ? ' | Bảo hành: ' + p.warrantyPeriodMonths + ' tháng' : '')).join('\n') + '\n' : '' }}

**YÊU CẦU PHÂN TÍCH:**

Hãy đưa ra gợi ý chi tiết về:
1. **Chẩn đoán vấn đề:** Nguyên nhân có thể, bộ phận có thể hỏng, mức độ nghiêm trọng
2. **Linh kiện đề xuất:** 
   - Dựa vào danh sách linh kiện có sẵn trong cửa hàng ở trên, đề xuất các linh kiện phù hợp để sửa chữa
   - Chỉ đề xuất các linh kiện có trong danh sách có sẵn
   - Ghi rõ mã linh kiện (PartCode), tên, số lượng cần thiết, và giá
   - Đảm bảo tương thích với dòng xe {{ $json.body.vehicleModel || 'này' }}
3. **Quy trình sửa chữa:** Các bước thực hiện, công cụ cần thiết, thời gian ước tính
4. **Lưu ý kỹ thuật:** Cảnh báo an toàn, điểm cần chú ý đặc biệt, khuyến nghị bảo dưỡng
5. **Ước tính chi phí:** Chi phí linh kiện (dựa trên giá trong danh sách), nhân công, tổng chi phí dự kiến

{{ $json.body.imageUrl ? '**Lưu ý:** Có ảnh minh chứng - Hãy phân tích kỹ hình ảnh để đưa ra chẩn đoán chính xác hơn.' : '' }}
```

---

## Cấu trúc n8n Workflow đề xuất:

1. **Webhook** - Nhận dữ liệu từ ASP.NET
2. **HTTP Request** - Lấy ảnh từ URL (GET imageUrl)
3. **Function** - Tạo prompt (hoặc dùng trực tiếp trong OpenAI node)
4. **OpenAI Chat** - Gửi prompt + ảnh tới AI
5. **Function** - Format response (tùy chọn)
6. **Respond to Webhook** - Trả kết quả về ASP.NET

---

## Lưu ý:

- Nếu dùng **OpenAI Vision API**, có thể gửi ảnh dưới dạng base64 trong message content
- Nếu dùng **Claude** hoặc model khác, điều chỉnh format prompt cho phù hợp
- Có thể thêm temperature và max_tokens để điều chỉnh độ sáng tạo và độ dài response

