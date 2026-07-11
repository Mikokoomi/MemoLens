# MemoLens - Định hướng redesign UI/UX

## 1. Mục đích

Phase 17A ghi lại hướng redesign trước khi sửa giao diện lớn. MemoLens hiện đã có nhiều tính năng MVP quan trọng, nhưng phần nhìn vẫn còn cảm giác khá Bootstrap và chưa đủ giống một nhật ký ký ức riêng tư.

Tài liệu này giúp:

- Tránh polish ngẫu nhiên từng trang mà không có cảm giác sản phẩm thống nhất.
- Định hình MemoLens như một không gian lưu giữ ký ức cá nhân, không phải dashboard quản trị.
- Giữ UI nhất quán với định hướng private-first, mobile-first và memory storytelling.
- Chuẩn bị nền thiết kế để sau này web MVC và Flutter/mobile có cùng tinh thần.

Phase này chỉ là định hướng thiết kế. Không implement CSS, Razor, font hay component mới trong phase này.

## 2. Đánh giá UI hiện tại

### Điểm đang làm tốt

- Bố cục hiện tại đã mobile-first hơn các phase đầu: navbar collapse, form có touch target tốt hơn, timeline card stack được trên màn hình nhỏ.
- Nội dung tiếng Việt đã rõ ràng hơn và phù hợp với sản phẩm riêng tư.
- Các trang chính đã có cấu trúc nhất quán: hero/header, card nội dung, form, empty state, footer.
- Timeline filter đã có cơ chế collapse nên không còn quá nặng trên mobile như trước.
- Footer đã nằm trong document flow và không che nội dung.

### Điểm còn generic

- Giao diện vẫn dựa nhiều vào Bootstrap card, button, form control nên chưa tạo được nhận diện riêng mạnh.
- Font hiện tại dùng stack hệ thống như `"Segoe UI", "Helvetica Neue", Arial`, đọc tốt nhưng chưa tạo cảm giác mềm, cá nhân và có chủ ý.
- Nhiều trang dùng cùng kiểu panel trắng, border và shadow nên dễ giống một admin dashboard nhẹ.
- Memory card hiện có nội dung ổn nhưng ảnh chưa đủ "photo-first"; cảm giác vẫn hơi giống record/list management.
- Form tạo/sửa memory còn giống form dữ liệu hơn là nơi viết câu chuyện.

### Trang cần chú ý nhiều nhất khi redesign

- **Home**: cần truyền cảm giác sản phẩm rõ hơn, ít giống landing Bootstrap.
- **Login/Register/Forgot/Reset**: cần mềm và đáng tin hơn, nhất là vì đây là app lưu ký ức riêng tư.
- **Timeline**: cần giống đang duyệt ký ức, không giống quản lý danh sách.
- **Memory Details**: cần cho ảnh và câu chuyện nhiều không gian hơn.
- **Create/Edit Memory**: cần biến form thành trải nghiệm ghi nhật ký, không chỉ nhập field.
- **Albums**: cần cảm giác bộ sưu tập cá nhân hơn, bớt giống grid card chung.

## 3. Cảm giác sản phẩm mong muốn

MemoLens nên có cảm giác:

- **Riêng tư**: người dùng thấy đây là không gian của mình, không phải nơi công khai.
- **Ấm áp**: màu, khoảng trắng và chữ gợi cảm giác dịu, gần gũi.
- **Nhẹ nhàng**: không tạo áp lực, không quá nhiều hiệu ứng hoặc màu chói.
- **Tập trung**: ảnh, câu chuyện, ngày tháng và cảm xúc là trung tâm.
- **Đáng tin**: auth, settings và privacy cần rõ ràng, bình tĩnh, không phô trương.
- **Hoài niệm nhưng hiện đại**: có chất journal/scrapbook nhẹ, nhưng không cũ kỹ hoặc trẻ con.
- **Sạch và bình yên**: ít nhiễu thị giác, ít chrome giao diện.

MemoLens không nên có cảm giác corporate/admin, mạng xã hội, app bán hàng, hay một template Bootstrap thay logo.

## 4. Hướng hình ảnh

Hướng đề xuất: **soft journal aesthetic**.

Nguyên tắc hình ảnh:

- Dùng nền warm neutral/off-white thay vì trắng gắt.
- Card memory nên lớn hơn, image-first hơn, để ảnh dẫn câu chuyện.
- Border và shadow nhẹ, ít hiệu ứng hover quá kỹ thuật.
- Thêm khoảng thở giữa các khối, đặc biệt ở mobile.
- Badge cảm xúc và tag nên nhỏ, mềm, thân thiện.
- Empty state nên có microcopy ấm, không chỉ báo "không có dữ liệu".
- Form nên được chia thành các phần có ý nghĩa như "Câu chuyện", "Thời gian", "Ảnh", "Thẻ".
- Giảm cảm giác dashboard bằng cách tránh các panel quá đều nhau, quá nhiều box nhỏ cùng lúc.

Không implement màu hoặc layout mới trong phase này.

## 5. Hướng typography

Font hiện tại nên được xem xét lại trong các phase redesign sau. Mục tiêu là đọc tốt tiếng Việt, có dấu rõ, không quá trang trí, và vẫn đủ thân thiện cho nhật ký cá nhân.

Các lựa chọn phù hợp:

| Font | Ưu điểm | Lưu ý |
| --- | --- | --- |
| Inter | Rất dễ đọc, hiện đại, UI-friendly, hỗ trợ tiếng Việt tốt. | Có thể hơi phổ thông nếu không phối màu/khoảng cách kỹ. |
| Be Vietnam Pro | Hỗ trợ tiếng Việt tốt, cảm giác Việt Nam rõ hơn, chuyên nghiệp. | Một số weight có thể tạo cảm giác hơi corporate nếu dùng quá nặng. |
| Noto Sans | Hỗ trợ ký tự rất tốt, an toàn cho đa ngôn ngữ. | Ít cá tính hơn, cần thiết kế xung quanh để không quá generic. |
| Nunito | Mềm, thân thiện, hợp app cá nhân. | Dễ thành hơi trẻ con nếu dùng heading quá tròn hoặc quá đậm. |
| Manrope | Sạch, hiện đại, có chút editorial. | Cần kiểm tra kỹ dấu tiếng Việt và cảm giác trên mobile. |

Đề xuất ban đầu:

- Dùng **Be Vietnam Pro** hoặc **Inter** cho UI chính.
- Có thể cân nhắc **Nunito** cho heading nếu muốn mềm hơn, nhưng phải tránh cảm giác trẻ con.
- Không dùng font quá decorative, script, serif khó đọc hoặc font không hỗ trợ tiếng Việt đầy đủ.
- Không thêm font file hoặc external font trong Phase 17A.

## 6. Hướng màu sắc

Palette nên bình tĩnh và ấm:

- Nền chính: warm off-white, ivory, paper-like neutral.
- Surface: trắng ngà hoặc kem rất nhạt, không trắng gắt.
- Text chính: charcoal/brown-black thay vì pure black.
- Text phụ: muted gray-green hoặc warm gray.
- Accent chính: muted olive/green, muted brown hoặc soft rose.
- Accent phụ: beige, clay, dusty rose, sage.
- Danger: đỏ trầm, không đỏ quá sáng.

Lưu ý:

- Giữ accessibility và contrast đủ tốt, nhất là form, button, link và validation.
- Không lạm dụng gradient.
- Không biến toàn bộ app thành một màu beige/cream đơn điệu.
- Dùng màu để hỗ trợ cảm xúc và phân cấp, không để trang thành bảng màu trang trí.

Không implement palette trong phase này.

## 7. Nguyên tắc layout

- Thiết kế cho mobile 390px trước, sau đó mở rộng lên tablet/desktop.
- Touch target nên đạt khoảng 44px ở các hành động quan trọng.
- Không để nội dung chạm sát mép màn hình điện thoại.
- Timeline nên tạo cảm giác lật lại ký ức, không phải quản lý record.
- Memory card nên ưu tiên ảnh, tiêu đề, ngày, cảm xúc và đoạn story ngắn.
- Filter phải hỗ trợ tìm lại ký ức nhưng không được chiếm toàn bộ trải nghiệm.
- Form tạo/sửa memory nên thân thiện, có nhóm rõ ràng, microcopy ngắn.
- Footer đơn giản, nằm trong normal flow, không fixed/absolute overlay.
- Desktop nên rộng rãi hơn, nhưng không biến thành dashboard nhiều cột dày đặc.

## 8. Kế hoạch redesign theo trang

### Home

- Làm rõ MemoLens là nơi lưu ký ức riêng tư, không phải social app.
- Hero nên mềm hơn, có cảm giác journal/memory.
- CTA chính nên dẫn người dùng vào Timeline hoặc tạo kỷ niệm sau login.
- Tránh layout quá marketing/template.

### Login/Register/Forgot/Reset

- Auth card cần yên tâm, mềm và rõ ràng.
- Copy nên nhấn mạnh quyền riêng tư và email confirmation.
- Link chuyển login/register/forgot phải dễ bấm trên mobile.
- Validation cần rõ nhưng không gay gắt.

### Timeline

- Memory card cần photo-first hơn.
- Filter nên nhẹ, giữ search dễ thấy và advanced filters gọn.
- Active filters nên hiện như pill mềm, dễ hiểu.
- Empty state nên khuyến khích tạo memory đầu tiên bằng giọng nhẹ nhàng.

### Create/Edit Memory

- Chia form thành các phần có ý nghĩa: câu chuyện, cảm xúc/ngày, địa điểm, ảnh, tag.
- Upload area cần giống vùng thêm ảnh riêng tư, không chỉ là input file mặc định.
- Existing image delete button trong Edit cần dễ bấm và không gây nhầm với xóa memory.
- Helper text nên tiếng Việt nhất quán, ngắn và ấm.

### Memory Details

- Ảnh và story cần là trung tâm.
- Gallery nên responsive hơn và có nhịp xem ảnh tự nhiên trên mobile.
- Metadata nên rõ nhưng không lấn át câu chuyện.
- Edit/Delete/Back cần dễ thấy nhưng không làm trang giống màn quản trị.

### Albums

- Album list nên giống bộ sưu tập cá nhân, không chỉ là grid record.
- Album detail nên thể hiện lý do album tồn tại và các memory bên trong bằng card giàu ảnh.
- Add memories nên rõ rằng chỉ thêm quan hệ vào album, không public/share.

### Trash

- Giữ cảm giác an toàn: đây là nơi khôi phục, không phải vùng nguy hiểm lớn.
- Cần phân biệt memory và album đã xóa bằng layout dễ scan.
- Khi sau này có permanent delete, cần thiết kế xác nhận thật rõ.

### Settings

- Trang settings nên tạo cảm giác kiểm soát dữ liệu cá nhân.
- Privacy notes nên ngắn, dễ hiểu, tránh quá technical.
- Các hành động tài khoản nên có thứ tự ưu tiên rõ.

### Privacy/Error Pages

- Privacy page nên giống lời cam kết sản phẩm, không giống legal/admin page.
- Error/access denied nên bình tĩnh, hướng người dùng về đúng nơi.

## 9. Quy tắc mobile-first

- Thiết kế và test ở 390px trước.
- Kiểm tra thêm 360px, 430px và 768px trước khi hoàn tất phase UI.
- Không horizontal overflow.
- Không text overlap.
- Không footer overlay hoặc chặn click.
- Button/link quan trọng cần dễ bấm bằng ngón tay.
- Auth pages không được chật hoặc bị footer chen.
- Filter timeline không được lấn át memory list.
- Image gallery phải tự thích nghi, không quá nhỏ.
- Form upload ảnh cần đủ rộng, helper text dễ đọc.
- Navbar mobile phải gọn, không chen chúc tài khoản và action chính.

## 10. Hướng component tương lai

Các component sau nên được định nghĩa dần trong Razor partials hoặc CSS helper, khi thật sự cần:

- **MemoryCard**: card ảnh + tiêu đề + ngày + feeling + đoạn story ngắn.
- **FeelingBadge**: badge cảm xúc mềm, màu có kiểm soát.
- **TagPill**: tag nhỏ, dễ scan, không quá nổi.
- **EmptyState**: trạng thái trống có lời nhắc nhẹ.
- **ImageGallery**: gallery responsive cho detail/edit.
- **PrivateNotice**: thông báo quyền riêng tư ngắn.
- **AuthCard**: khung auth nhất quán.
- **MobileNav pattern**: navbar mobile rõ, ít chen chúc.
- **FormSection**: nhóm field theo ý nghĩa, đặc biệt cho memory form.

Đây là hướng conceptual, chưa phải code.

## 11. Không nên làm

- Không biến MemoLens thành mạng xã hội.
- Không thêm feed, likes, comments, follows, public profile hoặc explore.
- Không thêm public sharing trong redesign MVP.
- Không thêm AI để làm UI trông "hiện đại".
- Không lạm dụng gradient, hiệu ứng nặng hoặc animation phức tạp.
- Không dùng chữ quá nhỏ trên mobile.
- Không dùng fixed footer overlay.
- Không làm UI giống admin dashboard.
- Không redesign toàn bộ app trong một commit khổng lồ.
- Không thay đổi auth, database, API hoặc business logic trong phase UI thuần.

## 12. Roadmap implement đề xuất

1. **Phase 17B: Design system tokens and base CSS**
   - Chốt font, màu, spacing, radius, shadow, button, form, link và base page rhythm.
   - Trạng thái: đã implement trong Phase 17B bằng CSS tokens và base Bootstrap overrides. Chưa redesign sâu từng trang.
2. **Phase 17C: Home/Auth UI redesign**
   - Làm lại Home, Login/Register/Forgot/Reset và status pages theo tone mới.
   - Trạng thái: đã implement trong Phase 17C cho Home và các trang auth/status. Chưa redesign Timeline, Memories, Albums, Trash hoặc Settings.
3. **Phase 17D: Timeline and Memory Card redesign**
   - Tập trung timeline, filter, active filters, MemoryCard và empty state.
   - Trạng thái: đã implement trong Phase 17D cho Timeline, search/filter panel, memory grid/card và empty states. Chưa redesign sâu Memory Details, Create/Edit, Albums, Trash hoặc Settings.
4. **Phase 17E: Apply Paper Note Default UI Template**
   - Áp dụng Paper Note làm style mặc định cho token nền, card, form, button, navbar, footer, Home/Auth và Timeline.
   - Trạng thái: đã implement trong Phase 17E. Chưa thêm 5 theme selectable, chưa thêm theme selector UI, chưa thêm `ThemePreference` và chưa tạo migration.
5. **Phase 17F: Memory Details and Gallery redesign**
   - Làm rõ story/photo-first detail, gallery, edit/delete/back actions.
   - Trạng thái: đã implement trong Phase 17F cho Memory Details, gallery responsive, empty image state, detail actions và Delete confirmation. Chưa redesign sâu Create/Edit, Albums, Trash hoặc Settings.
6. **Phase 17G: Create/Edit Memory Form redesign**
   - Làm form tạo/sửa memory giống trải nghiệm viết nhật ký hơn, gồm story, cảm xúc, ngày, địa điểm, ảnh và tag.
   - Trạng thái: đã implement trong Phase 17G cho Create/Edit Memory forms, upload note, validation presentation, action row và existing image edit grid. Chưa redesign Albums, Trash hoặc Settings.
7. **Phase 17H: Albums/Trash/Settings redesign**
   - Làm lại album list/detail/add, trash và settings theo cùng design system.
8. **Phase 17I: Final responsive QA**
   - Kiểm tra 360/390/430/768, desktop, auth, CRUD, upload, search/filter và tests.

Ghi chú: selectable themes có thể là hướng tương lai sau MVP, nhưng chưa thuộc Phase 17E. MemoLens hiện chỉ dùng Paper Note làm giao diện mặc định.

## 13. Acceptance criteria cho redesign tương lai

- Mobile 360px, 390px, 430px và 768px không overlap.
- Không horizontal overflow.
- Footer không che nội dung hoặc chặn click.
- Navbar mobile dễ dùng.
- Vietnamese text nhất quán, có dấu đầy đủ.
- Font hỗ trợ tiếng Việt tốt.
- Timeline vẫn dễ tìm kiếm/filter nhưng memory card là trung tâm.
- Create/Edit Memory dễ dùng trên điện thoại.
- Gallery ảnh trong Details hiển thị tự nhiên.
- Auth/API/database behavior không đổi nếu phase chỉ là UI.
- Không migration mới cho phase UI.
- `dotnet build` pass.
- `dotnet test` pass.
- NuGet vulnerability audit sạch.
- Product identity private-first vẫn rõ ràng.
