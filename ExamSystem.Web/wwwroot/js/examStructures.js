        // 1. BIẾN TOÀN CỤC (Lưu giá trị cũ để so sánh)
        let previousValues = {};

        // 2. KHỞI TẠO (DOMContentLoaded)
        document.addEventListener("DOMContentLoaded", function() {
            const inputs = document.querySelectorAll('.auto-save-input');

            inputs.forEach(input => {
                // Lưu giá trị ban đầu
                const rowId = input.closest('tr').getAttribute('data-id');
                const field = input.getAttribute('data-field');
                const key = `${rowId}_${field}`;
                previousValues[key] = input.value;

                // Cập nhật lại khi focus
                input.addEventListener('focus', function() {
                    previousValues[key] = this.value;
                });

                // Sự kiện Auto Save
                input.addEventListener('blur', function() { handleAutoSave(this); });
                if (input.tagName === 'SELECT') {
                    input.addEventListener('change', function() { handleAutoSave(this); });
                }

                // Xóa lỗi khi gõ lại
                input.addEventListener('input', function() {
                    this.classList.remove('is-invalid');
                });
            });
        });

        // 3. HÀM XỬ LÝ LƯU (AUTO SAVE) - SỬA LẠI ĐỂ HIỆN THÔNG BÁO LỖI
        function handleAutoSave(element) {
            const row = element.closest('tr');
            const partId = row.getAttribute('data-id');
            const statusSpan = row.querySelector('.save-status');

            const orderInput = row.querySelector('[data-field="orderIndex"]');
            const skillInput = row.querySelector('[data-field="skillType"]');
            const nameInput = row.querySelector('[data-field="name"]');
            const descInput = row.querySelector('[data-field="description"]');

            // Dirty Check
            const currentVal = element.value;
            const fieldName = element.getAttribute('data-field');
            const key = `${partId}_${fieldName}`;
            if (previousValues[key] === currentVal) return; // Không đổi -> Không làm gì

            // Validate Client
            let isValid = true;
            nameInput.classList.remove('is-invalid');
            orderInput.classList.remove('is-invalid');

            if (!nameInput.value.trim()) { nameInput.classList.add('is-invalid'); isValid = false; }
            if (orderInput.value <= 0) { orderInput.classList.add('is-invalid'); isValid = false; }

            if (!isValid) {
                statusSpan.innerHTML = '<i class="bi bi-exclamation-circle text-danger"></i>';
                return;
            }

            statusSpan.innerHTML = '<div class="spinner-border spinner-border-sm text-primary"></div>';

            const formData = new FormData();
            formData.append('partId', partId);
            formData.append('orderIndex', orderInput.value);
            formData.append('skillType', skillInput.value);
            formData.append('name', nameInput.value.trim());
            formData.append('description', descInput.value);

            fetch('/Admin/ExamStructures/UpdatePartAjax', {
                method: 'POST',
                body: formData
            })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    // Cập nhật giá trị cũ
                    previousValues[`${partId}_orderIndex`] = orderInput.value;
                    previousValues[`${partId}_skillType`] = skillInput.value;
                    previousValues[`${partId}_name`] = nameInput.value;
                    previousValues[`${partId}_description`] = descInput.value;

                    setTimeout(() => {
                        statusSpan.innerHTML = '<i class="bi bi-check-lg text-success fw-bold"></i>';
                        statusSpan.title = "Đã lưu";
                        row.classList.add('bg-success-subtle');
                        setTimeout(() => row.classList.remove('bg-success-subtle'), 500);
                    }, 400);
                } else {
                    // LỖI: BẬT ALERT ĐỂ NGƯỜI DÙNG BIẾT TẠI SAO LỖI
                    statusSpan.innerHTML = '<i class="bi bi-exclamation-triangle text-danger"></i>';
                    statusSpan.title = data.message;

                    alert("Lỗi lưu dữ liệu: " + data.message); // <--- QUAN TRỌNG: Hiện thông báo lỗi

                    if(data.message.includes("Thứ tự")) orderInput.classList.add('is-invalid');
                    if(data.message.includes("Tên")) nameInput.classList.add('is-invalid');
                }
            })
            .catch(error => {
                console.error(error);
                statusSpan.innerHTML = '<i class="bi bi-wifi-off text-danger"></i>';
            });
        }

        // 4. HÀM THÊM MỚI (ADD NEW)
        function addNewPart(btnElement) {
            const structureId = document.getElementById('add_structureId').value;
            const orderInput = document.getElementById('add_orderIndex');
            const skillInput = document.getElementById('add_skillType');
            const nameInput = document.getElementById('add_name');
            const descInput = document.getElementById('add_description');
            const errorBox = document.getElementById('add-error-msg');

            errorBox.classList.add('d-none');
            orderInput.classList.remove('is-invalid');
            nameInput.classList.remove('is-invalid');

            if (!nameInput.value.trim()) {
                nameInput.classList.add('is-invalid');
                nameInput.focus();
                return;
            }
            if (orderInput.value <= 0) {
                orderInput.classList.add('is-invalid');
                orderInput.focus();
                return;
            }

            const formData = new FormData();
            formData.append('structureId', structureId);
            formData.append('orderIndex', orderInput.value);
            formData.append('skillType', skillInput.value);
            formData.append('name', nameInput.value.trim());
            formData.append('description', descInput.value);

            const originalText = btnElement.innerHTML;
            btnElement.disabled = true;
            btnElement.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Đang thêm...';

            fetch('/Admin/ExamStructures/AddPartAjax', {
                method: 'POST',
                body: formData
            })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    window.location.reload();
                } else {
                    btnElement.disabled = false;
                    btnElement.innerHTML = originalText;

                    errorBox.textContent = "Lỗi: " + data.message;
                    errorBox.classList.remove('d-none');

                    if(data.message.includes("Thứ tự")) orderInput.classList.add('is-invalid');
                    if(data.message.includes("Tên")) nameInput.classList.add('is-invalid');
                }
            })
            .catch(error => {
                console.error(error);
                btnElement.disabled = false;
                btnElement.innerHTML = originalText;
                alert("Lỗi kết nối server!");
            });
        }
        // =========================================================
        // 5. CÔNG CỤ: TẠO MẪU NHANH (GENERATE)
        // =========================================================
        function generateSkill(skillType) {
            const structureId = document.getElementById('add_structureId').value;

            // Hiệu ứng loading đơn giản (block UI)
            if(!confirm("Hệ thống sẽ tự động thêm các phần thi mẫu vào cuối danh sách. Tiếp tục?")) return;

            const formData = new FormData();
            formData.append('structureId', structureId);
            formData.append('skillType', skillType);

            fetch('/Admin/ExamStructures/GenerateStructureAjax', { method: 'POST', body: formData })
            .then(res => res.json())
            .then(data => {
                if (data.success) {                   
                    window.location.reload();
                } else {
                    alert("Lỗi: " + data.message);
                }
            })
            .catch(err => alert("Lỗi kết nối server!"));
        }

        // =========================================================
        // 6. CÔNG CỤ: XÓA NHANH THEO NHÓM (CLEAR)
        // =========================================================
        function clearSkill(skillType) {
            const structureId = document.getElementById('add_structureId').value;

            if(!confirm("CẢNH BÁO: Hành động này sẽ XÓA TẤT CẢ các phần thi thuộc kỹ năng bạn chọn.\nDữ liệu không thể khôi phục. Bạn có chắc chắn không?")) return;

            const formData = new FormData();
            formData.append('structureId', structureId);
            formData.append('skillType', skillType);

            fetch('/Admin/ExamStructures/ClearStructureAjax', { method: 'POST', body: formData })
            .then(res => res.json())
            .then(data => {
                if (data.success) {
                  showNotification(data.message, true);
            // Xóa phần tử khỏi giao diện (hoặc reload lại trang sau 1 giây)
            setTimeout(() => {
                window.location.reload();
            }, 1000);
                } else {
                   showNotification(data.message, false);
                }
            })
            .catch(err => alert("Lỗi kết nối server!"));
}

function confirmDelete(id, name) {
    Swal.fire({
        title: 'Xóa cấu trúc đề thi?',
        html: `Bạn có chắc chắn muốn xóa cấu trúc <b>${name}</b>?<br><span class='text-danger'>Lưu ý: Toàn bộ các phần thi bên trong cũng sẽ bị xóa vĩnh viễn!</span>`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Đồng ý xóa',
        cancelButtonText: 'Hủy bỏ'
    }).then((result) => {
        if (result.isConfirmed) {
            // Gửi request POST lên server
            fetch(`/Admin/ExamStructures/Delete/${id}`, {
                method: 'POST'
            })
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        Swal.fire(
                            'Đã xóa!',
                            'Cấu trúc đã được xóa thành công.',
                            'success'
                        ).then(() => {
                            // Tải lại trang để cập nhật danh sách
                            window.location.reload();
                        });
                    } else {
                        Swal.fire('Lỗi!', data.message, 'error');
                    }
                })
                .catch(error => {
                    Swal.fire('Lỗi!', 'Không thể kết nối đến máy chủ.', 'error');
                });
        }
    });
}
