function copyToClipboard(text) {
    // Tạo đường dẫn đầy đủ
    var fullUrl = window.location.origin + text;
    navigator.clipboard.writeText(fullUrl).then(function () {
        // Dùng SweetAlert2 nếu có, không thì alert thường
        if (typeof Swal !== 'undefined') {
            Swal.fire({
                icon: 'success',
                title: 'Đã sao chép!',
                text: fullUrl,
                timer: 1500,
                showConfirmButton: false
            });
        } else {
            alert('Đã sao chép link: ' + fullUrl);
        }
    }, function (err) {
        console.error('Lỗi sao chép: ', err);
    });
}

//function confirmDelete(id, title) {
//    Swal.fire({
//        title: 'Xác nhận xóa?',
//        html: `Bạn có chắc chắn muốn xóa dữ liệu <b>${title}</b>?<br><span class="text-danger small">Hành động này không thể hoàn tác!</span>`,
//        icon: 'warning',
//        showCancelButton: true,
//        confirmButtonColor: '#d33',
//        cancelButtonColor: '#6c757d',
//        confirmButtonText: 'Vâng, Xóa ngay',
//        cancelButtonText: 'Hủy bỏ',
//        customClass: {
//            confirmButton: 'rounded-pill px-4 fw-bold',
//            cancelButton: 'rounded-pill px-4 fw-bold',
//            popup: 'rounded-4'
//        }
//    }).then((result) => {
//        if (result.isConfirmed) {
//            // Hiển thị loading cho đẹp trong lúc chờ server xử lý
//            Swal.fire({
//                title: 'Đang xóa...',
//                allowOutsideClick: false,
//                didOpen: () => { Swal.showLoading(); }
//            });
//            // Tìm cái form ẩn tương ứng với ID và bấm nút Submit
//            document.getElementById('delete-form-' + id).submit();
//        }
//    });
//}

// Thay thế hàm cũ trong admin.js bằng hàm này
function confirmDelete(id, title, controller) {
    // Lấy Token chống giả mạo (CSRF) từ trong trang HTML
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

    Swal.fire({
        title: 'Xác nhận xóa?',
        html: `Bạn có chắc chắn muốn xóa dữ liệu <b>${title}</b>?<br><span class="text-danger small">Hành động này không thể hoàn tác!</span>`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Vâng, Xóa ngay',
        cancelButtonText: 'Hủy bỏ',
        customClass: {
            confirmButton: 'rounded-pill px-4 fw-bold',
            cancelButton: 'rounded-pill px-4 fw-bold',
            popup: 'rounded-4'
        }
    }).then((result) => {
        if (result.isConfirmed) {
            Swal.fire({
                title: 'Đang xóa...',
                allowOutsideClick: false,
                didOpen: () => { Swal.showLoading(); }
            });

            // Tự động tạo một form ảo bằng Javascript và Submit
            const form = document.createElement('form');
            form.method = 'POST';
            // Đảm bảo URL trỏ đúng đến hàm Delete trong Controller của anh
            form.action = '/Admin/'+controller+'/Delete/' + id;

            // Nhét token vào form
            const tokenInput = document.createElement('input');
            tokenInput.type = 'hidden';
            tokenInput.name = '__RequestVerificationToken';
            tokenInput.value = token;
            form.appendChild(tokenInput);

            document.body.appendChild(form);
            form.submit();
        }
    });
}

// --- LOGIC CHECKBOX VÀ XÓA HÀNG LOẠT MỚI ---
document.addEventListener('DOMContentLoaded', function () {
    const checkAll_UsersBtn = document.getElementById('checkAll_Users');
    const userCheckboxes = document.querySelectorAll('.user-checkbox');
    const bulkContainer = document.getElementById('bulkActionContainer');
    const selectedCountDisplay = document.getElementById('selectedCount');

    function updateBulkActionUI() {
        const checkedCount = document.querySelectorAll('.user-checkbox:checked').length;
        if (checkedCount > 0) {
            selectedCountDisplay.innerText = checkedCount;
            bulkContainer.classList.remove('d-none');
        } else {
            bulkContainer.classList.add('d-none');
        }

        // Đồng bộ nút checkAll_Users
        checkAll_UsersBtn.checked = (checkedCount === userCheckboxes.length && userCheckboxes.length > 0);
    }

    // Bắt sự kiện Check All
    if (checkAll_UsersBtn) {
        checkAll_UsersBtn.addEventListener('change', function () {
            userCheckboxes.forEach(cb => cb.checked = this.checked);
            updateBulkActionUI();
        });
    }

    // Bắt sự kiện Check từng dòng
    userCheckboxes.forEach(cb => {
        cb.addEventListener('change', updateBulkActionUI);
    });
});

function confirmDeleteMultiple(itemType, apiUrl) {
    const checkedBoxes = document.querySelectorAll('.user-checkbox:checked');
    const idsToDelete = Array.from(checkedBoxes).map(cb => cb.value);

    // Nếu không có checkbox nào được chọn thì thông báo nhẹ
    if (idsToDelete.length === 0) {
        Swal.fire({
            title: 'Chưa chọn mục nào!',
            text: `Vui lòng chọn ít nhất một ${itemType} để xóa.`,
            icon: 'info',
            confirmButtonColor: '#f47b25',
            customClass: { popup: 'rounded-4' }
        });
        return;
    }

    // Hiển thị hộp thoại xác nhận
    Swal.fire({
        title: 'CẢNH BÁO!',
        html: `Bạn đang chuẩn bị xóa <b>${idsToDelete.length}</b> ${itemType}.<br/><span class="text-danger">Toàn bộ dữ liệu liên quan sẽ bị xóa vĩnh viễn.</span> Bạn có chắc không?`,
        icon: 'error',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'XÓA TẤT CẢ',
        cancelButtonText: 'Hủy bỏ',
        customClass: { popup: 'rounded-4' }
    }).then((result) => {
        if (result.isConfirmed) {

            // 1. Hiển thị loading mượt mà
            Swal.update({
                title: 'Đang xử lý...',
                html: 'Vui lòng chờ trong giây lát',
                showConfirmButton: false,
                showCancelButton: false
            });
            Swal.showLoading();

            // 2. Gọi Fetch API bằng URL động
            fetch(apiUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(idsToDelete)
            })
                .then(res => res.json())
                .then(data => {
                    // 3. Xử lý kết quả trả về
                    if (data.success) {
                        window.location.reload();
                    } else {
                        Swal.fire('Lỗi xóa dữ liệu', data.message || `Không thể xóa ${itemType} lúc này.`, 'error');
                    }
                })
                .catch(err => {
                    console.error("Lỗi Fetch API:", err);
                    Swal.fire('Lỗi kết nối', 'Không thể kết nối đến máy chủ. Vui lòng thử lại sau.', 'error');
                });
        }
    });
}


setInterval(() => {
    const now = new Date();
    const timeString = now.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' }) + ' - ' + now.toLocaleDateString('vi-VN');
    document.getElementById('liveClock').innerText = timeString;
}, 60000); // Cập nhật mỗi 1 phút