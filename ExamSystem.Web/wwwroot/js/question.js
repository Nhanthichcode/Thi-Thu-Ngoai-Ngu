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

    // --- LOGIC MỚI: TRÁO ĐỔI NÚT ẨN/HIỆN ---
    const btnExpand = document.getElementById('btnExpandAll');
    const btnCollapse = document.getElementById('btnCollapseAll');

    if (action === 'show') {
        // Nếu vừa bấm "Mở rộng" -> Ẩn nút "Mở rộng", Hiện nút "Thu gọn"
        if (btnExpand) btnExpand.classList.add('d-none');
        if (btnCollapse) btnCollapse.classList.remove('d-none');
    } else {
        // Nếu vừa bấm "Thu gọn" -> Ẩn nút "Thu gọn", Hiện nút "Mở rộng"
        if (btnExpand) btnExpand.classList.remove('d-none');
        if (btnCollapse) btnCollapse.classList.add('d-none');
    }
}

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

            // Ẩn tất cả section tài nguyên
            $(".form-section").hide();
            $("#block-reading").hide();
            $("#block-listening").hide();
            $("#block-image").hide();

            // LOGIC CHO TRANG EDIT
            if (skillSelectEdit) {
                $("#block-answers").show();
                $("#block-no-answers").hide();
                if (skill == "1") $("#block-listening").show();
                else if (skill == "2") $("#block-reading").show();
                else if (skill == "3") { $("#block-answers").hide(); $("#block-no-answers").show(); }
                else if (skill == "4") { $("#block-image").show(); $("#block-answers").hide(); $("#block-no-answers").hide(); }
            }
            // LOGIC CHO TRANG CREATE
            else if (skillSelectCreate) {
                if (skill == "1" || skill == "2" || skill == "4") {
                    $("#leftPane").show();
                    $("#rightPane").removeClass("col-lg-12").addClass("col-lg-7");
                    if (skill == "1") $("#section-listening").fadeIn(300);
                    if (skill == "2") $("#section-reading").fadeIn(300);
                    if (skill == "4") $("#section-image").fadeIn(300);
                } else {
                    $("#leftPane").hide();
                    $("#rightPane").removeClass("col-lg-7").addClass("col-lg-12");
                }

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
// 3. TRANG BATCH EDIT (SỬA HÀNG LOẠT)
// =========================================================================
var deletedIds = [];
function markDelete(btn, id) {
    if (confirm("Bạn muốn xóa câu hỏi này khỏi bài?")) {
        deletedIds.push(id);
        document.getElementById('DeletedIdsInput').value = deletedIds.join(',');
        $(btn).closest('.q-item').fadeOut(300, function () { $(this).remove(); });
    }
}

function addBlankQuestion() {
    var template = document.getElementById('new-q-template');
    if (!template) return;

    var batchCurrentIndex = document.querySelectorAll('#questions-container .q-item').length + deletedIds.length + 100; // Cộng dồn để né ID cũ
    var html = template.innerHTML.replace(/{INDEX}/g, batchCurrentIndex);
    document.getElementById('questions-container').insertAdjacentHTML('beforeend', html);
}

function showError(element, message) {
    var errorAlert = document.getElementById('error-alert');
    if (errorAlert) {
        document.getElementById('error-message').innerText = message;
        errorAlert.classList.remove('d-none');
    }

    if (element) {
        element.classList.add('is-invalid');
        element.focus();
        var headerOffset = 150;
        var elementPosition = element.getBoundingClientRect().top;
        var offsetPosition = elementPosition + window.pageYOffset - headerOffset;

        window.scrollTo({
            top: offsetPosition,
            behavior: "smooth"
        });

        element.oninput = function () {
            element.classList.remove('is-invalid');
            if (errorAlert) errorAlert.classList.add('d-none');
        };
    }
}

function submitForm() {
    var errorAlert = document.getElementById('error-alert');
    if (errorAlert) errorAlert.classList.add('d-none');
    document.querySelectorAll('.is-invalid').forEach(el => el.classList.remove('is-invalid'));

    var title = document.getElementById('ResourceTitle');
    if (title && !title.value.trim()) {
        showError(title, "Vui lòng nhập Tiêu đề bài!");
        return;
    }

    var content = document.getElementById('ResourceContent');
    if (content && !content.value.trim()) {
        showError(content, "Vui lòng nhập Nội dung hoặc Transcript!");
        return;
    }

    var questions = document.querySelectorAll('.q-item');
    if (questions.length === 0) {
        showError(null, "Bài này chưa có câu hỏi nào. Vui lòng thêm ít nhất 1 câu hỏi!");
        return;
    }

    var isValidQuestions = true;
    for (var i = 0; i < questions.length; i++) {
        if (questions[i].style.display === 'none') continue;
        var qContent = questions[i].querySelector('.q-content-input');
        if (qContent && !qContent.value.trim()) {
            showError(qContent, "Nội dung câu hỏi không được để trống!");
            isValidQuestions = false;
            break;
        }
    }

    if (isValidQuestions) {
        document.getElementById('batchEditForm').submit();
    }
}

// =========================================================================
// 4. TRANG IMPORT EXCEL (CẬP NHẬT LOAD LẠI FILE VÀ THU GỌN CHA/CON)
// =========================================================================
const fileInput = document.getElementById('fileInput');

if (fileInput) {
    // [QUAN TRỌNG] FIX LỖI KHÔNG CẬP NHẬT DỮ LIỆU KHI CHỌN TRÙNG TÊN FILE
    fileInput.addEventListener('click', function () {
        this.value = null; // Quét sạch file cũ trong bộ nhớ để ép trình duyệt load lại từ đầu
    });

    fileInput.addEventListener('change', function (e) {
        const file = e.target.files[0];
        if (!file) return;

        document.getElementById('emptyState').classList.add('d-none');
        document.getElementById('previewSection').classList.add('d-none');
        document.getElementById('loadingArea').classList.remove('d-none');

        if (typeof XLSX === 'undefined') {
            Swal.fire('Lỗi', 'Chưa tải được thư viện Excel (SheetJS). Vui lòng F5 lại trang.', 'error');
            return;
        }

        const reader = new FileReader();
        reader.onload = function (e) {
            try {
                const data = new Uint8Array(e.target.result);
                const workbook = XLSX.read(data, { type: 'array' });
                const worksheet = workbook.Sheets[workbook.SheetNames[0]];
                const jsonData = XLSX.utils.sheet_to_json(worksheet, { header: 1, defval: "" });

                if (jsonData.length < 6 || !jsonData[5][0]) {
                    throw new Error("Không tìm thấy mã định danh tại ô A6. Vui lòng tải file mẫu chuẩn.");
                }

                const detectedQuestionType = jsonData[5][0].toString().trim();
                document.getElementById('questionTypeBadge').innerText = "Loại: " + detectedQuestionType;
                document.getElementById('questionTypeBadge').classList.remove('d-none');

                const tbody = document.getElementById('tableBody');
                tbody.innerHTML = '';

                let validCount = 0;
                let invalidCount = 0;

                let currentParentIndex = -1;
                let isCurrentParentValid = false;

                for (let i = 7; i < jsonData.length; i++) {
                    const row = jsonData[i];
                    if (!row || row.length === 0 || (!row[0] && !row[1] && !row[2])) continue;

                    let isValid = true;
                    let errorMsg = "";
                    let displayHtml = "";

                    let isParent = false;
                    let isChild = false;

                    // ----------------------------------------------------------------
                    if (detectedQuestionType === "TYPE_GRAMMAR") {
                        const qContent = row[0] ? row[0].toString().trim() : '';
                        displayHtml = `<div class="text-wrap">${qContent}</div>`;
                        if (!qContent) { isValid = false; errorMsg += "Câu hỏi bị trống; "; }
                        if (!row[6]) { isValid = false; errorMsg += "Thiếu vị trí đáp án đúng; "; }
                    }
                    else if (detectedQuestionType === "TYPE_WRITING" || detectedQuestionType === "TYPE_SPEAKING") {
                        const qContent = row[0] ? row[0].toString().trim() : '';
                        displayHtml = `<div class="text-wrap">${qContent}</div>`;
                        if (!qContent) { isValid = false; errorMsg += "Đề bài bị trống; "; }
                    }
                    // ----------------------------------------------------------------
                    // ĐỌC / NGHE (CÓ CHA - CON)
                    else if (detectedQuestionType === "TYPE_READING" || detectedQuestionType === "TYPE_LISTENING") {
                        const title = row[0] ? row[0].toString().trim() : '';
                        const passage = row[1] ? row[1].toString().trim() : '';
                        const qContent = row[2] ? row[2].toString().trim() : '';

                        // Phân tích bài Cha
                        if (title) {
                            isParent = true;
                            currentParentIndex = i;

                            // Gắn thêm class 'parent-toggle-icon' vào icon để nhận diện Đóng/Mở Tất Cả
                            const toggleBtn = `<button class="btn btn-sm btn-light border py-0 px-1 ms-2" onclick="window.toggleChildRows(${i}, this)" title="Thu gọn/Mở rộng câu con"><i class="bi bi-chevron-down text-muted parent-toggle-icon"></i></button>`;

                            displayHtml += `<div class="fw-bold text-primary mb-1 d-flex align-items-center"><i class="bi bi-journal-text me-2"></i> BÀI: ${title} ${toggleBtn}</div>`;

                            if (detectedQuestionType === "TYPE_READING" && !passage) {
                                isValid = false; errorMsg += "Bài đọc thiếu Nội dung; ";
                                isCurrentParentValid = false;
                            } else {
                                isCurrentParentValid = true;
                            }
                        }

                        // Phân tích câu Con
                        if (qContent) {
                            isChild = true;
                            displayHtml += `<div class="${title ? 'ms-4 mt-2' : ''} text-wrap"><i class="bi bi-arrow-return-right text-muted me-2"></i> ${qContent}</div>`;

                            if (!title && !isCurrentParentValid) {
                                isValid = false; errorMsg += "Bài Cha phía trên bị lỗi, câu hỏi này mồ côi; ";
                            }
                            let validAns = 0;
                            for (let j = 4; j <= 7; j++) if (row[j] && row[j].toString().trim()) validAns++;
                            if (validAns < 2) { isValid = false; errorMsg += "Chưa đủ 2 đáp án; "; }
                            if (row[8] && !row[3 + parseInt(row[8])]) { isValid = false; errorMsg += `Đáp án đúng rỗng; `; }
                        }
                    }

                    if (isValid) validCount++; else invalidCount++;

                    const statusHtml = isValid
                        ? '<span class="badge bg-success bg-opacity-10 text-success border border-success rounded-pill px-3">Hợp lệ</span>'
                        : `<div class="text-danger small fw-bold" style="white-space: normal; line-height: 1.5;"><i class="bi bi-exclamation-triangle-fill me-1"></i>Lỗi: ${errorMsg}</div>`;

                    let cbHtml = '';
                    if (isValid) {
                        if (isParent && (detectedQuestionType === "TYPE_READING" || detectedQuestionType === "TYPE_LISTENING")) {
                            cbHtml = `<input class="form-check-input row-checkbox parent-checkbox shadow-sm" type="checkbox" value="${i}" data-index="${i}" checked style="cursor: pointer;">`;
                        } else if (isChild && !isParent) {
                            cbHtml = `<input class="form-check-input row-checkbox child-checkbox shadow-sm" type="checkbox" value="${i}" data-parent="${currentParentIndex}" checked style="cursor: pointer;">`;
                        } else {
                            cbHtml = `<input class="form-check-input row-checkbox shadow-sm" type="checkbox" value="${i}" checked style="cursor: pointer;">`;
                        }
                    } else {
                        cbHtml = `<i class="bi bi-x-circle-fill text-danger fs-5"></i>`;
                    }

                    const tr = document.createElement('tr');
                    tr.className = isValid ? "" : "bg-danger bg-opacity-10";

                    // Gắn nhãn câu con để JS biết đường thu gọn
                    if (isChild && !isParent && currentParentIndex !== -1) {
                        tr.classList.add(`child-of-${currentParentIndex}`);
                    }

                    tr.innerHTML = `
                        <td class="text-center align-middle">${cbHtml}</td>
                        <td class="text-center fw-bold align-middle">${i + 1}</td>
                        <td class="align-middle py-2">${displayHtml}</td>
                        <td class="align-middle" style="min-width: 150px;">${statusHtml}</td>
                    `;
                    tbody.appendChild(tr);
                }

                document.getElementById('validCount').innerText = validCount;
                document.getElementById('invalidCount').innerText = invalidCount;
                updateSelectedCountBadge(validCount);

                const btnImport = document.getElementById('btnImport');
                btnImport.disabled = (validCount === 0);

                document.getElementById('loadingArea').classList.add('d-none');
                document.getElementById('previewSection').classList.remove('d-none');

                // Đặt lại trạng thái nút Thu gọn về ban đầu khi load file mới
                window.isAllCollapsed = false;
                document.getElementById('childRowsIcon').className = 'bi bi-arrows-collapse me-1';
                document.getElementById('childRowsText').innerText = "Thu gọn câu con";

            } catch (error) {
                document.getElementById('loadingArea').classList.add('d-none');
                document.getElementById('emptyState').classList.remove('d-none');
                Swal.fire('Lỗi định dạng', error.message, 'error');
            }
        };
        reader.readAsArrayBuffer(file);
    });

    // Checkbox Cha-Con Logic
    document.getElementById('tableBody').addEventListener('change', function (e) {
        if (e.target.classList.contains('row-checkbox')) {
            if (e.target.classList.contains('parent-checkbox')) {
                const parentIdx = e.target.getAttribute('data-index');
                const isChecked = e.target.checked;
                document.querySelectorAll(`.child-checkbox[data-parent="${parentIdx}"]:not(:disabled)`).forEach(cb => {
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

            const total = document.querySelectorAll('.row-checkbox:not(:disabled)').length;
            const checked = document.querySelectorAll('.row-checkbox:checked').length;
            document.getElementById('checkAll_Import').checked = (total === checked && total > 0);

            updateSelectedCountBadge(checked);
        }
    });

    document.getElementById('checkAll_Import').addEventListener('change', function (e) {
        const isChecked = e.target.checked;
        document.querySelectorAll('.row-checkbox:not(:disabled)').forEach(cb => {
            cb.checked = isChecked;
        });
        updateSelectedCountBadge(document.querySelectorAll('.row-checkbox:checked').length);
    });

    function updateSelectedCountBadge(count) {
        document.getElementById('selectedCountBadge').innerText = `Sẽ lưu: ${count} dòng`;
        const btnImport = document.getElementById('btnImport');
        btnImport.disabled = (count === 0);
        btnImport.innerHTML = `<i class="bi bi-database-fill-up me-2"></i> LƯU ${count} DỮ LIỆU HỢP LỆ`;
    }

    // Submit lưu dữ liệu
    document.getElementById('btnImport').addEventListener('click', function () {
        const file = document.getElementById('fileInput').files[0];
        if (!file) return;

        const checkedBoxes = document.querySelectorAll('.row-checkbox:checked');
        const selectedIndices = Array.from(checkedBoxes).map(cb => cb.value).join(',');

        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
        const formData = new FormData();
        formData.append("file", file);
        formData.append("mode", "save");
        formData.append("selectedRows", selectedIndices); 

        Swal.fire({
            title: 'Đang xử lý dữ liệu...',
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
                    Swal.fire('Hoàn tất!', data.message, 'success').then(() => window.location.href = data.redirectUrl);
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