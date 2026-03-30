//const questionSectionMap = @Html.Raw(System.Text.Json.JsonSerializer.Serialize(questionSectionMap));
let currentIndex = 0;

// --- 0. TOGGLE SIDEBAR (ẨN/HIỆN MENU) ---
function toggleSidebar() {
    const sidebar = document.getElementById('sidebarContainer');
    const icon = document.getElementById('sidebarToggleIcon');
    sidebar.classList.toggle('collapsed');

    if (sidebar.classList.contains('collapsed')) {
        icon.classList.replace('bi-chevron-right', 'bi-chevron-left');
    } else {
        icon.classList.replace('bi-chevron-left', 'bi-chevron-right');
    }
}

// Cấu hình ban đầu
window.onload = function () { goToSection(0); };

// --- 1. THEO DÕI VÀ ĐIỀU HƯỚNG BẢN ĐỒ CÂU HỎI ---
function jumpToQuestion(qIdx) {
    let secIdx = questionSectionMap[qIdx];
    if (currentIndex !== secIdx) {
        goToSection(secIdx);
    }
    setTimeout(() => {
        let qEl = document.getElementById('q-wrapper-' + qIdx);
        if (qEl) {
            qEl.scrollIntoView({ behavior: 'smooth', block: 'center' });
            highlightMapBtn(qIdx);
        }
    }, 100);
}

function highlightMapBtn(qIdx) {
    document.querySelectorAll('.map-btn').forEach(b => b.classList.remove('active-view'));
    let btn = document.getElementById('map-btn-' + qIdx);
    if (btn) btn.classList.add('active-view');
}

function markAnswered(qIdx, hasValue) {
    let btn = document.getElementById('map-btn-' + qIdx);
    if (btn) {
        if (hasValue) btn.classList.add('answered');
        else btn.classList.remove('answered');
    }
}

// Tự động phát hiện câu đang xem khi cuộn chuột
let observer = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
        if (entry.isIntersecting) {
            let qId = entry.target.id.split('-').pop();
            highlightMapBtn(qId);
        }
    });
}, { root: document.getElementById('mainContent'), threshold: 0.5 });

document.querySelectorAll('[id^="q-wrapper-"]').forEach(el => observer.observe(el));

// --- 2. ĐIỀU HƯỚNG TABS GOM NHÓM ---
function goToSection(index) {
    // Đổi content
    document.querySelectorAll('.exam-section').forEach(el => el.classList.remove('active'));
    document.getElementById('section-' + index).classList.add('active');

    // Xóa toàn bộ active ở các nút tab và nhóm
    document.querySelectorAll('.tab-btn').forEach(el => el.classList.remove('active'));
    document.querySelectorAll('.skill-group').forEach(el => el.classList.remove('active-skill'));

    // Kích hoạt tab được chọn và nhóm chứa nó
    let activeBtn = document.querySelector('.part-btn-' + index);
    if (activeBtn) {
        activeBtn.classList.add('active');
        activeBtn.closest('.skill-group').classList.add('active-skill');
    }

    currentIndex = index;
    document.getElementById('mainContent').scrollTop = 0;
}

// --- 3. ĐỒNG HỒ VÀ NỘP BÀI ---
//let timeLeft = @Model.DurationMinutes * 60;
setInterval(() => {
    if (timeLeft <= 0) { submitExam(); return; }
    let m = Math.floor(timeLeft / 60);
    let s = timeLeft % 60;
    document.getElementById('countdown').innerText = `${m < 10 ? '0' + m : m}:${s < 10 ? '0' + s : s}`;
    timeLeft--;
}, 1000);

function submitExam() {
    if (confirm("Bạn có chắc chắn muốn nộp bài?")) {
        document.body.style.opacity = '0.5';
        document.body.style.pointerEvents = 'none';
        document.getElementById('examForm').submit();
    }
}

// --- 4. GHI ÂM (SPEAKING) ---
let recorders = {};
let chunks = {};
async function toggleRecording(qId, qIdx) {
    const ui = document.getElementById(`recorder-${qId}`);
    const btn = ui.querySelector('.btn-record');
    const timerEl = document.getElementById(`timer-${qId}`);
    const statusText = ui.querySelector('.status-text');
    const fileInput = document.getElementById(`file-input-${qId}`);

    if (recorders[qId] && recorders[qId].state === "recording") {
        recorders[qId].stop();
        clearInterval(ui.dataset.timer);
        return;
    }

    if (fileInput.files.length > 0 && !confirm("Ghi âm lại sẽ xóa bản cũ?")) return;
    fileInput.files = new DataTransfer().files;

    try {
        const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
        const mediaRecorder = new MediaRecorder(stream);
        recorders[qId] = mediaRecorder;
        chunks[qId] = [];

        mediaRecorder.ondataavailable = e => { if (e.data.size > 0) chunks[qId].push(e.data); };
        mediaRecorder.onstop = () => {
            const blob = new Blob(chunks[qId], { type: 'audio/webm' });
            const file = new File([blob], `ans_${qId}.webm`, { type: "audio/webm" });
            const container = new DataTransfer();
            container.items.add(file);
            fileInput.files = container.files;

            document.getElementById(`playback-${qId}`).src = URL.createObjectURL(blob);
            document.getElementById(`playback-${qId}`).classList.remove('d-none');

            btn.classList.replace('btn-danger', 'btn-outline-success');
            btn.innerHTML = '<i class="bi bi-arrow-counterclockwise fs-5"></i>';

            statusText.innerText = "Đã lưu bản thu thành công.";
            statusText.classList.add('text-success');
            statusText.classList.remove('text-danger', 'text-dark');
            stream.getTracks().forEach(track => track.stop());

            markAnswered(qIdx, true);
        };

        mediaRecorder.start();
        btn.classList.replace('btn-outline-danger', 'btn-danger');
        btn.innerHTML = '<i class="bi bi-stop-fill fs-5 text-white"></i>';

        statusText.innerText = "Đang ghi âm...";
        statusText.classList.add('text-danger');
        statusText.classList.remove('text-success', 'text-dark');

        let recTime = 0;
        timerEl.innerText = "00:00";
        ui.dataset.timer = setInterval(() => {
            recTime++;
            let rm = Math.floor(recTime / 60); let rs = recTime % 60;
            timerEl.innerText = `${rm < 10 ? '0' + rm : rm}:${rs < 10 ? '0' + rs : rs}`;
        }, 1000);
    } catch (err) { alert("Vui lòng cấp quyền Micro cho trình duyệt!"); }
}

// --- XỬ LÝ ĐẾM TỪ CHO BÀI VIẾT ---
function handleWritingInput(questionId, qIdx, text) {
    // Xóa các khoảng trắng thừa ở đầu/cuối và các khoảng trắng kép ở giữa
    let cleanText = text.trim().replace(/\s+/g, ' ');

    // Đếm số từ
    let wordCount = cleanText === "" ? 0 : cleanText.split(' ').length;

    // Hiển thị ra màn hình
    let countDisplay = document.getElementById('wordcount-' + questionId);
    if (countDisplay) {
        countDisplay.innerText = wordCount;
    }

    // Đánh dấu vào bản đồ câu hỏi (sidebar) nếu đã gõ nội dung
    markAnswered(qIdx, wordCount > 0);
}