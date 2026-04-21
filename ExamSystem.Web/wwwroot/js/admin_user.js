        let parsedData = [];

        // 1. Xử lý đọc file Excel khi người dùng chọn
        document.getElementById('excelFile').addEventListener('change', function(e) {
            const file = e.target.files[0];
            if (!file) return;

            const reader = new FileReader();
            reader.onload = function(e) {
                const data = new Uint8Array(e.target.result);
                const workbook = XLSX.read(data, {type: 'array'});

                // Đọc sheet đầu tiên
                const firstSheetName = workbook.SheetNames[0];
                const worksheet = workbook.Sheets[firstSheetName];

                // Lấy dữ liệu dạng mảng 2 chiều (bỏ qua dòng tiêu đề đầu tiên)
                const jsonData = XLSX.utils.sheet_to_json(worksheet, {header: 1});

                parsedData = [];
                const tbody = document.getElementById('tableBody');
                tbody.innerHTML = '';

                // Bắt đầu đọc từ dòng thứ 2 (index = 1)
                for (let i = 1; i < jsonData.length; i++) {
                    const row = jsonData[i];
                    // Bỏ qua dòng trống hoàn toàn
                    if (!row || row.length === 0 || (row[0] == null && row[1] == null && row[2] == null)) continue;

                    const email = row[0] ? row[0].toString().trim() : '';
                    const fullName = row[1] ? row[1].toString().trim() : '';
                    const password = row[2] ? row[2].toString().trim() : '';

                    // Kiểm tra tính hợp lệ cơ bản
                    let isValid = true;
                    let errorMsg = '';

                    if (!email) { isValid = false; errorMsg += 'Tên đăng nhập không được bỏ trống; '; }
                    if (!fullName) { isValid = false; errorMsg += 'Thiếu Họ tên; '; }
                    if (!password || password.length < 6) { isValid = false; errorMsg += 'Mật khẩu < 6 ký tự; '; }

                    parsedData.push({
                        index: i + 1,
                        email: email,
                        fullName: fullName,
                        password: password,
                        isValid: isValid,
                        errorMsg: errorMsg
                    });

                    // Render ra HTML
                    const tr = document.createElement('tr');
                   
                    tr.className = isValid ? "" : "text-danger";

                    tr.innerHTML = `
                                    <td class="text-center">
                                        <input class="form-check-input checkbox-xl row-checkbox shadow-sm" type="checkbox" value="${parsedData.length - 1}" ${isValid ? 'checked' : 'disabled'}>
                                    </td>
                                    <td class="text-center fw-bold">${i + 1}</td>
    
                                    <!-- LOẠI BỎ class text-dark ở các dòng dưới đây -->
                                    <td class="fw-bold">${email || '<i>- Trống -</i>'}</td>
                                    <td>${fullName || '<i>- Trống -</i>'}</td>
                                    <td>${password || '<i>- Trống -</i>'}</td>
    
                                    <td>
                                        ${isValid
                                                            ? '<span class="badge bg-success bg-opacity-10 text-success border border-success rounded-pill"><i class="bi bi-check-circle-fill me-1"></i>Hợp lệ</span>'
                                                            : `<span class="badge bg-danger bg-opacity-10 text-danger border border-danger rounded-pill"><i class="bi bi-exclamation-circle-fill me-1"></i>${errorMsg}</span>`}
                                    </td>
                                `;
                    tbody.appendChild(tr);
                }

                // Hiển thị khung xem trước
                document.getElementById('previewSection').classList.remove('d-none');
                updateSelectedCount();

                // Reset file input để có thể chọn lại file cũ nếu cần
                document.getElementById('excelFile').value = '';
            };
            reader.readAsArrayBuffer(file);
        });

        // 2. Xử lý Checkbox
document.getElementById('checkAll_Import').addEventListener('change', function(e) {
            const isChecked = e.target.checked;
            document.querySelectorAll('.row-checkbox:not(:disabled)').forEach(cb => {
                cb.checked = isChecked;
            });
            updateSelectedCount();
        });

        document.getElementById('tableBody').addEventListener('change', function(e) {
            if (e.target.classList.contains('row-checkbox')) {
                updateSelectedCount();
                // Bỏ check "Check All" nếu có 1 ô bị bỏ check
                const total = document.querySelectorAll('.row-checkbox:not(:disabled)').length;
                const checked = document.querySelectorAll('.row-checkbox:checked').length;
                document.getElementById('checkAll_Import').checked = (total === checked && total > 0);
            }
        });

        function updateSelectedCount() {
            const count = document.querySelectorAll('.row-checkbox:checked').length;
            document.getElementById('selectedCountBadge').innerText = `Đã chọn: ${count} dòng`;
            document.getElementById('btnImport').disabled = count === 0;
        }

        // 3. Xử lý gửi dữ liệu lên Server
        document.getElementById('btnImport').addEventListener('click', function() {
            const checkedBoxes = document.querySelectorAll('.row-checkbox:checked');
            if (checkedBoxes.length === 0) return;

            // Lấy danh sách các dòng được chọn
            const usersToImport = [];
            checkedBoxes.forEach(cb => {
                const dataIndex = parseInt(cb.value);
                const user = parsedData[dataIndex];
                usersToImport.push({
                    Email: user.email,
                    FullName: user.fullName,
                    Password: user.password
                });
            });

            // Lấy CSRF Token
            const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

            // Hiển thị Loading SweetAlert2
            Swal.fire({
                title: 'Đang xử lý dữ liệu...',
                text: `Đang tạo ${usersToImport.length} tài khoản, vui lòng chờ trong giây lát!`,
                allowOutsideClick: false,
                didOpen: () => { Swal.showLoading(); }
            });

            // Gửi dữ liệu bằng Fetch API
            fetch('/Admin/Users/ProcessImport', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(usersToImport)
            })
            .then(res => res.json())
            .then(data => {
                if (data.success) {
                    let msg = `Đã tạo thành công <b>${data.successCount}</b> tài khoản.`;
                    if (data.errorCount > 0) {
                        msg += `<br/><br/><span class="text-danger">Có ${data.errorCount} tài khoản bị lỗi (có thể do trùng Tên đăng nhập và mật khẩu).</span>`;
                    }

                    Swal.fire({
                        icon: data.errorCount > 0 ? 'warning' : 'success',
                        title: 'Hoàn tất Import!',
                        html: msg,
                        confirmButtonText: 'Quay về Danh sách',
                        confirmButtonColor: '#10b981'
                    }).then(() => {
                        window.location.href = '/Admin/Users/Index';
                    });
                } else {
                    Swal.fire('Lỗi', data.message, 'error');
                }
            })
            .catch(err => {
                Swal.fire('Lỗi hệ thống', 'Không thể kết nối tới máy chủ.', 'error');
            });
        });