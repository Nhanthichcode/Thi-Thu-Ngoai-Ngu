// =========================================================================
// 1. TRANG QUẢN LÝ CÂU HỎI (INDEX.CSHTML)
// =========================================================================
document.addEventListener("DOMContentLoaded", function () {
    // Tự động submit khi chọn Level
    const levelSelect = document.getElementById('levelSelect');
    if (levelSelect) {
        levelSelect.addEventListener('change', function () {
            document.getElementById('filterForm').submit();
        });
    }

    // Đảo chiều mũi tên khi click mở từng nhóm
    document.querySelectorAll('[data-bs-toggle="collapse"]').forEach(el => {
        el.addEventListener('click', function () {
            let icon = this.querySelector('.collapse-icon');
            if (icon) {
                if (icon.classList.contains('bi-chevron-down')) {
                    icon.classList.replace('bi-chevron-down', 'bi-chevron-up');
                } else {
                    icon.classList.replace('bi-chevron-up', 'bi-chevron-down');
                }
            }
        });
    });
});

function toggleAll(action) {
    var collapseElementList = [].slice.call(document.querySelectorAll('.group-collapse-content'));

    collapseElementList.forEach(function (collapseEl) {
        var bsCollapse = bootstrap.Collapse.getInstance(collapseEl);
        if (!bsCollapse) {
            bsCollapse = new bootstrap.Collapse(collapseEl, { toggle: false });
        }

        let header = collapseEl.previousElementSibling;
        let icon = header ? header.querySelector('.collapse-icon') : null;

        if (action === 'show') {
            bsCollapse.show();
            if (icon) {
                icon.classList.replace('bi-chevron-right', 'bi-chevron-down');
                icon.classList.replace('bi-chevron-up', 'bi-chevron-down');
            }
        } else {
            bsCollapse.hide();
            if (icon) {
                icon.classList.replace('bi-chevron-down', 'bi-chevron-up');
            }
        }
    });

    // LƯU VÀO CACHE: Ghi nhớ trạng thái 'show' hoặc 'hide'
    localStorage.setItem('exam-collapse-state', action);

    // Xử lý ẩn/hiện nút bấm (nếu bạn có dùng 2 nút Mở rộng/Thu gọn riêng biệt)
    const btnExpand = document.getElementById('btnExpandAll');
    const btnCollapse = document.getElementById('btnCollapseAll');
    if (action === 'show') {
        if (btnExpand) btnExpand.classList.add('d-none');
        if (btnCollapse) btnCollapse.classList.remove('d-none');
    } else {
        if (btnExpand) btnExpand.classList.remove('d-none');
        if (btnCollapse) btnCollapse.classList.add('d-none');
    }
}
document.addEventListener("DOMContentLoaded", function () {
    // Đọc trạng thái từ bộ nhớ cache
    const savedState = localStorage.getItem('exam-collapse-state');

    // Nếu có dữ liệu lưu trữ (người dùng đã từng bấm nút)
    if (savedState) {
        // Delay nhẹ 100ms để đảm bảo HTML và Bootstrap đã render xong rồi mới ép trạng thái
        setTimeout(() => {
            toggleAll(savedState);
        }, 100);
    }
});

// =========================================================================
// 2. TRANG SOẠN THẢO / CHỈNH SỬA (CREATE.CSHTML & EDIT.CSHTML)
// =========================================================================
$(document).ready(function () {
    const skillSelectCreate = document.getElementById("SkillSelect");
    const skillSelectEdit = document.getElementById("SkillTypeHidden");

    // Chỉ chạy logic nếu đang ở trang Create hoặc Edit
    if (skillSelectCreate || skillSelectEdit) {

        function updateQuestionUI() {
            var skill = skillSelectCreate ? $(skillSelectCreate).val() : $(skillSelectEdit).val();

            // 1. Reset trạng thái: Ẩn tất cả các form section
            $(".form-section").hide();
            $("#block-reading").hide();
            $("#block-listening").hide();
            $("#block-image").hide();

            // 2. LOGIC QUẢN LÝ CỘT TRÁI & PHẢI 
            if (skill == "3" || skill == "4") {
                $("#rightPane").hide(); // Ẩn câu trắc nghiệm phụ 
                $("#leftPane").show();
                $("#leftPane").removeClass("col-lg-5").addClass("col-lg-12");
            } else {
                $("#rightPane").show();
                $("#rightPane").removeClass("col-lg-12").addClass("col-lg-7");
                $("#leftPane").show();
                $("#leftPane").removeClass("col-lg-12").addClass("col-lg-5");
            }

            // 3. LOGIC TRANG EDIT (Giữ nguyên)
            if (skillSelectEdit) {
                $("#block-answers").show();
                $("#block-no-answers").hide();

                if (skill == "1") $("#block-listening").show();
                else if (skill == "2") $("#block-reading").show();
                else if (skill == "3") {
                    $("#block-answers").hide();
                    $("#block-no-answers").show();
                }
                else if (skill == "4") {
                    $("#block-image").show();
                    $("#block-answers").hide();
                    $("#block-no-answers").hide();
                }
            }
            // 4. LOGIC TRANG CREATE
            else if (skillSelectCreate) {
                // Gọi hiển thị Form Section tương ứng với từng kỹ năng
                if (skill == "1") $("#section-listening").fadeIn(300);
                if (skill == "2") $("#section-reading").fadeIn(300);

                // GỌI HIỂN THỊ SECTION VIẾT (Vừa thêm ở HTML)
                if (skill == "3") $("#section-writing").fadeIn(300);

                if (skill == "4") $("#section-image").fadeIn(300);

                // Quản lý đáp án & Giải thích phụ (Giữ nguyên)
                if (skill == "3" || skill == "4") {
                    $(".answer-block").addClass("d-none");
                    $(".no-answer-block").removeClass("d-none");
                } else {
                    $(".answer-block").removeClass("d-none");
                    $(".no-answer-block").addClass("d-none");
                }

                if (skill == "4") $(".explain-block").addClass("d-none");
                else $(".explain-block").removeClass("d-none");
            }
        }

        if (skillSelectCreate) {
            $(skillSelectCreate).change(function () {
                updateQuestionUI();
            });
        }

        // Khởi chạy giao diện lần đầu
        updateQuestionUI();

        // Tự động thêm câu hỏi đầu tiên ở trang Create
        if (document.getElementById('q-template')) {
            addQuestion();
        }

        // AJAX tải nội dung tài nguyên
        var apiUrl = '/Admin/Questions/GetResourceDetails';
        $("#SelectReadingId").change(function () {
            var id = $(this).val();
            if (id) {
                $.get(apiUrl + "?type=reading&id=" + id, function (d) {
                    if (d.success) {
                        $("#ReadingPreview").html(d.content);
                        $("#ReadingPreviewContainer").hide().removeClass('d-none').slideDown(300);
                    }
                }).fail(function () { alert("Lỗi khi tải dữ liệu bài đọc!"); });
            } else {
                $("#ReadingPreviewContainer").slideUp(200, function () { $(this).addClass('d-none'); });
            }
        });

        $("#SelectListeningId").change(function () {
            var id = $(this).val();
            if (id) {
                $.get(apiUrl + "?type=listening&id=" + id, function (d) {
                    if (d.success) {
                        $("#AudioPlayer").attr('src', d.audioUrl);
                        $("#TranscriptPreview").html(d.transcript ? d.transcript : "<i class='text-muted'>Chưa có bản dịch.</i>");
                        $("#ListeningPreviewContainer").hide().removeClass('d-none').slideDown(300);
                    }
                }).fail(function () { alert("Lỗi khi tải dữ liệu bài nghe!"); });
            } else {
                $("#ListeningPreviewContainer").slideUp(200, function () { $(this).addClass('d-none'); $("#AudioPlayer").attr('src', ''); });
            }
        });

        // Preview ảnh
        $("#imageInput").change(function () {
            var file = this.files[0];
            if (file) {
                var reader = new FileReader();
                reader.onload = function (e) {
                    $("#img-preview").attr("src", e.target.result);
                    $("#preview-container").removeClass("d-none");
                }
                reader.readAsDataURL(file);
            } else {
                $("#preview-container").addClass("d-none");
            }
        });
    }
});

// Các hàm cho trang Create
var qCounter = 0;
function addQuestion() {
    var template = document.getElementById('q-template');
    if (!template) return;
    var html = template.innerHTML.replace(/{INDEX}/g, qCounter);
    var $newItem = $(html).hide();
    $("#questions-container").append($newItem);

    // Kích hoạt event cập nhật UI để ẩn hiện đúng các khối
    const skillSelectCreate = document.getElementById("SkillSelect");
    if (skillSelectCreate) $(skillSelectCreate).trigger('change');

    $newItem.fadeIn(300);
    qCounter++;
    updateNumbers();
}

function removeQ(btn) {
    if (confirm('Xóa câu hỏi này?')) {
        $(btn).closest(".q-item").fadeOut(300, function () {
            $(this).remove();
            updateNumbers();
        });
    }
}

function updateNumbers() {
    var total = 0;
    $(".q-item").each(function (i) {
        $(this).find(".q-num").text(i + 1);
        total++;
    });
    $("#totalQuestionsBadge").text(total + " câu");
}

window.previewImage = function (input) {
    if (input.files && input.files[0]) {
        var reader = new FileReader();
        reader.onload = function (e) { $('#imgPreview').attr('src', e.target.result).removeClass('d-none'); }
        reader.readAsDataURL(input.files[0]);
    }
}

// Các hàm cho trang Edit
function deleteCurrentMedia() {
    if (confirm("Bạn có chắc chắn muốn xóa ảnh này? Thay đổi sẽ được lưu sau khi cập nhật.")) {
        $("#deleteImageFlag").val("true");
        $("#MediaUrl").val("");
        $("#current-image-wrapper").remove();
        $("#current-image-container").append('<div class="text-muted small py-4 border rounded bg-light" id="no-image-placeholder">Đã xóa ảnh</div>');
        $("#imageInput").val('');
        $("#preview-container").addClass("d-none");
    }
}

// =========================================================================
// 4. TRANG IMPORT EXCEL (CẬP NHẬT LOAD LẠI FILE VÀ THU GỌN CHA/CON)
// =========================================================================
const fileInput = document.getElementById('fileInput');

if (fileInput) {
    // 1. Reset file input khi click để ép trình duyệt load lại nếu chọn trùng file
    fileInput.addEventListener('click', function () {
        this.value = null;
    });

    // 2. SỰ KIỆN THAY ĐỔI FILE: Gửi lên Server để Phân tích & Check trùng DB
    fileInput.addEventListener('change', function (e) {
        const file = e.target.files[0];
        if (!file) return;

        // Hiển thị trạng thái Loading
        document.getElementById('emptyState').classList.add('d-none');
        document.getElementById('previewSection').classList.add('d-none');
        document.getElementById('loadingArea').classList.remove('d-none');

        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
        const formData = new FormData();
        formData.append("file", file);
        formData.append("mode", "check"); // Chế độ kiểm tra (Preview), không lưu

        fetch('/Admin/Questions/Import', {
            method: 'POST',
            headers: { 'RequestVerificationToken': token },
            body: formData
        })
            .then(res => res.json())
            .then(data => {
                document.getElementById('loadingArea').classList.add('d-none');

                // Trường hợp lỗi hệ thống hoặc file không đúng định dạng
                if (!data.isSuccess && (!data.rowPreviews || data.rowPreviews.length === 0)) {
                    Swal.fire('Lỗi phân tích', data.message, 'error');
                    document.getElementById('emptyState').classList.remove('d-none');
                    return;
                }

                // Hiển thị Badge loại câu hỏi (Reading, Listening, Grammar...)
                const badge = document.getElementById('questionTypeBadge');
                badge.innerText = "Loại: " + (data.detectedType || "Không xác định");
                badge.classList.remove('d-none');

                const tbody = document.getElementById('tableBody');
                tbody.innerHTML = '';

                // 3. DUYỆT DỮ LIỆU TỪ SERVER TRẢ VỀ ĐỂ VẼ BẢNG PREVIEW
                data.rowPreviews.forEach(item => {
                    let displayHtml = "";
                    let cbHtml = "";

                    // Xử lý hiển thị Nội dung (Cha/Con)
                    if (item.isParent) {
                        const toggleBtn = `<button class="btn btn-sm btn-light border py-0 px-1 ms-2" onclick="window.toggleChildRows(${item.rowIndex}, this)" title="Thu gọn/Mở rộng"><i class="bi bi-chevron-down text-muted parent-toggle-icon"></i></button>`;
                        displayHtml = `<div class="fw-bold text-primary mb-1 d-flex align-items-center"><i class="bi bi-journal-text me-2"></i> ${item.content} ${toggleBtn}</div>`;
                    } else {
                        const indentClass = item.parentIndex !== -1 ? "ms-4 mt-2" : "";
                        displayHtml = `<div class="${indentClass} text-wrap"><i class="bi bi-arrow-return-right text-muted me-2"></i> ${item.content}</div>`;
                    }

                    // Xử lý Trạng thái & Checkbox
                    const statusHtml = item.isValid
                        ? '<span class="badge bg-success bg-opacity-10 text-success border border-success rounded-pill px-3">Hợp lệ</span>'
                        : `<div class="text-danger small fw-bold" style="white-space: normal; line-height: 1.5;"><i class="bi bi-exclamation-triangle-fill me-1"></i>Lỗi: ${item.errorMessage}</div>`;

                    if (item.isValid) {
                        const parentAttr = item.parentIndex !== -1 ? `data-parent="${item.parentIndex}"` : `data-index="${item.rowIndex}"`;
                        const typeClass = item.isParent ? "parent-checkbox" : "child-checkbox";

                        cbHtml = `<input class="form-check-input checkbox-xl row-checkbox ${typeClass} shadow-sm" type="checkbox" value="${item.rowIndex - 1}" ${parentAttr} checked style="cursor: pointer; width:1.2em; height:1.2em;">`;
                    } else {
                        cbHtml = `<i class="bi bi-x-circle-fill text-danger fs-5" title="${item.errorMessage}"></i>`;
                    }

                    const tr = document.createElement('tr');
                    tr.className = item.isValid ? "" : "bg-danger bg-opacity-10";
                    if (item.parentIndex !== -1) tr.classList.add(`child-of-${item.parentIndex}`);

                    tr.innerHTML = `
                    <td class="text-center align-middle">${cbHtml}</td>
                    <td class="text-center fw-bold align-middle">${item.rowIndex}</td>
                    <td class="align-middle py-2">${displayHtml}</td>
                    <td class="align-middle" style="min-width: 200px;">${statusHtml}</td>
                `;
                    tbody.appendChild(tr);
                });

                // Cập nhật các con số thống kê
                document.getElementById('validCount').innerText = data.validCount;
                document.getElementById('invalidCount').innerText = data.invalidCount;
                updateSelectedCountBadge(data.validCount);

                document.getElementById('previewSection').classList.remove('d-none');

                // Reset trạng thái toggle
                window.isAllCollapsed = false;
                document.getElementById('childRowsIcon').className = 'bi bi-arrows-collapse me-1';
                document.getElementById('childRowsText').innerText = "Thu gọn câu con";
            })
            .catch(error => {
                console.error("Fetch Error:", error);
                document.getElementById('loadingArea').classList.add('d-none');
                document.getElementById('emptyState').classList.remove('d-none');
                Swal.fire('Lỗi kết nối', 'Không thể gửi file lên server để phân tích.', 'error');
            });
    });

    // 4. LOGIC CHECKBOX (CHA-CON)
    document.getElementById('tableBody').addEventListener('change', function (e) {
        if (e.target.classList.contains('row-checkbox')) {
            if (e.target.classList.contains('parent-checkbox')) {
                const parentIdx = e.target.getAttribute('data-index');
                const isChecked = e.target.checked;
                document.querySelectorAll(`.child-checkbox[data-parent="${parentIdx}"]`).forEach(cb => {
                    cb.checked = isChecked;
                });
            }
            else if (e.target.classList.contains('child-checkbox')) {
                const parentIdx = e.target.getAttribute('data-parent');
                const parentCb = document.querySelector(`.parent-checkbox[data-index="${parentIdx}"]`);
                if (parentCb) {
                    const checkedChildren = document.querySelectorAll(`.child-checkbox[data-parent="${parentIdx}"]:checked`).length;
                    parentCb.checked = (checkedChildren > 0);
                }
            }
            const checked = document.querySelectorAll('.row-checkbox:checked').length;
            updateSelectedCountBadge(checked);
        }
    });

    function updateSelectedCountBadge(count) {
        document.getElementById('selectedCountBadge').innerText = `Sẽ lưu: ${count} dòng`;
        const btnImport = document.getElementById('btnImport');
        btnImport.disabled = (count === 0);
        btnImport.innerHTML = `<i class="bi bi-database-fill-up me-2"></i> LƯU ${count} DÒNG`;
    }

    // 5. SUBMIT LƯU DỮ LIỆU
    document.getElementById('btnImport').addEventListener('click', function () {
        const file = document.getElementById('fileInput').files[0];
        const checkedBoxes = document.querySelectorAll('.row-checkbox:checked');
        const selectedIndices = Array.from(checkedBoxes).map(cb => cb.value).join(',');

        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
        const formData = new FormData();
        formData.append("file", file);
        formData.append("mode", "save");
        formData.append("selectedRows", selectedIndices);

        Swal.fire({
            title: 'Đang xử lý lưu dữ liệu...',
            allowOutsideClick: false,
            didOpen: () => { Swal.showLoading(); }
        });

        fetch('/Admin/Questions/Import', {
            method: 'POST',
            headers: { 'RequestVerificationToken': token },
            body: formData
        })
            .then(res => res.json())
            .then(data => {
                if (data.isSuccess) {
                    Swal.fire('Thành công!', data.message, 'success').then(() => window.location.href = data.redirectUrl);
                } else {
                    Swal.fire('Lỗi nhập liệu', data.message, 'error');
                }
            });
    });
}
// ==========================================
// CÁC HÀM UI: THU GỌN / MỞ RỘNG CHA CON
// ==========================================

window.isAllCollapsed = false;

// 1. Nút "Thu gọn / Mở rộng" TẤT CẢ
window.toggleAllChildRows = function () {
    window.isAllCollapsed = !window.isAllCollapsed;

    const childRows = document.querySelectorAll('tr[class*="child-of-"]');
    const parentIcons = document.querySelectorAll('.parent-toggle-icon');
    const mainIcon = document.getElementById('childRowsIcon');
    const mainText = document.getElementById('childRowsText');

    if (window.isAllCollapsed) {
        // Đang Thu gọn
        childRows.forEach(row => row.classList.add('d-none'));
        parentIcons.forEach(icon => icon.classList.replace('bi-chevron-down', 'bi-chevron-right'));
        mainIcon.classList.replace('bi-arrows-collapse', 'bi-arrows-expand');
        mainText.innerText = "Mở rộng câu con";
    } else {
        // Đang Mở rộng
        childRows.forEach(row => row.classList.remove('d-none'));
        parentIcons.forEach(icon => icon.classList.replace('bi-chevron-right', 'bi-chevron-down'));
        mainIcon.classList.replace('bi-arrows-expand', 'bi-arrows-collapse');
        mainText.innerText = "Thu gọn câu con";
    }
};

// 2. Nút mũi tên đóng mở TỪNG Bài Cha
window.toggleChildRows = function (parentIndex, btn) {
    const children = document.querySelectorAll(`.child-of-${parentIndex}`);
    const icon = btn.querySelector('i');
    const isExpanded = icon.classList.contains('bi-chevron-down');

    if (isExpanded) {
        icon.classList.replace('bi-chevron-down', 'bi-chevron-right');
        children.forEach(row => row.classList.add('d-none'));
        btn.classList.add('bg-secondary', 'text-white');
    } else {
        icon.classList.replace('bi-chevron-right', 'bi-chevron-down');
        children.forEach(row => row.classList.remove('d-none'));
        btn.classList.remove('bg-secondary', 'text-white');
    }
};

document.addEventListener("DOMContentLoaded", function () {

    const container = document.querySelector('.question-list-container');
    const bulkContainer = document.getElementById('bulkDeleteContainer');
    const btnBulkDelete = document.getElementById('btnBulkDelete');
    const countSpan = document.getElementById('bulkDeleteCount');

    if (container && bulkContainer) {

        container.addEventListener('change', function (e) {

            // 1. NẾU BẤM NÚT "CHỌN CẢ NHÓM" TRÊN HEADER
            if (e.target.classList.contains('check-all-group')) {
                // Tìm cái thẻ Card chứa nút này
                const card = e.target.closest('.card');
                // Lấy tất cả các checkbox câu con bên trong và tích/bỏ tích theo nút Header
                const childCheckboxes = card.querySelectorAll('.question-checkbox');
                childCheckboxes.forEach(cb => cb.checked = e.target.checked);
            }

            // 2. NẾU BẤM NÚT "CHỌN TỪNG CÂU"
            else if (e.target.classList.contains('question-checkbox')) {
                const card = e.target.closest('.card');
                const checkAllBtn = card.querySelector('.check-all-group');
                const total = card.querySelectorAll('.question-checkbox').length;
                const checked = card.querySelectorAll('.question-checkbox:checked').length;

                // Cập nhật trạng thái của nút Header (Nếu tích đủ con thì cha sáng, nếu thiếu thì cha tắt)
                if (checkAllBtn) {
                    checkAllBtn.checked = (total === checked && total > 0);
                    // Dòng này tạo hiệu ứng dấu trừ (-) rất đẹp nếu chị chỉ chọn một vài câu con
                    checkAllBtn.indeterminate = (checked > 0 && checked < total);
                }
            }

            // Dù bấm nút nào thì cũng gọi hàm cập nhật giao diện
            updateBulkDeleteUI();
        });

        // HÀM ĐẾM SỐ LƯỢNG: CHỈ ĐẾM CÁC CÂU HỎI CON
        function updateBulkDeleteUI() {
            // Chỉ đếm những checkbox có class 'question-checkbox'
            const totalChecked = document.querySelectorAll('.question-checkbox:checked').length;
            if (totalChecked > 0) {
                bulkContainer.classList.remove('d-none');
                countSpan.innerText = totalChecked;
            } else {
                bulkContainer.classList.add('d-none');
            }
        }

        if (btnBulkDelete) {
            btnBulkDelete.addEventListener('click', function () {
                // LẤY ID CỦA CÁC CÂU HỎI CON ĐỂ GỬI LÊN SERVER
                const checkedBoxes = document.querySelectorAll('.question-checkbox:checked');
                const idsToDelete = Array.from(checkedBoxes).map(cb => parseInt(cb.value));

                if (idsToDelete.length === 0) return;

                Swal.fire({
                    title: 'Xác nhận xóa hàng loạt?',
                    html: `Bạn đang chọn xóa <b>${idsToDelete.length}</b> câu hỏi.<br><span class="text-danger small">Hành động này không thể hoàn tác!</span>`,
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonColor: '#d33',
                    cancelButtonColor: '#6c757d',
                    confirmButtonText: 'Vâng, Xóa tất cả!',
                    cancelButtonText: 'Hủy bỏ',
                    customClass: {
                        confirmButton: 'rounded-pill px-4',
                        cancelButton: 'rounded-pill px-4',
                        popup: 'rounded-4'
                    }
                }).then((result) => {
                    if (result.isConfirmed) {
                        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

                        Swal.fire({
                            title: 'Đang xóa...',
                            allowOutsideClick: false,
                            didOpen: () => { Swal.showLoading(); }
                        });

                        fetch('/Admin/Questions/BulkDelete', {
                            method: 'POST',
                            headers: {
                                'Content-Type': 'application/json',
                                'RequestVerificationToken': token
                            },
                            body: JSON.stringify(idsToDelete)
                        })
                            .then(res => res.json())
                            .then(data => {
                                if (data.success) {
                                    Swal.fire('Thành công', data.message, 'success').then(() => location.reload());
                                } else {
                                    Swal.fire('Lỗi', data.message, 'error');
                                }
                            });
                    }
                });
            });
        }
    }
});

