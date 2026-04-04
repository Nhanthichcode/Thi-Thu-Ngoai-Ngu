using ExamSystem.Core.Entities;
using ExamSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.EntityFrameworkCore;

namespace ExamSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")] // 2. Thêm Attribute này
    [Authorize(Roles = "Admin, Teacher")]
    public class ExamStructuresController : Controller
    {
        private readonly AppDbContext _context;

        public ExamStructuresController(AppDbContext context)
        {
            _context = context;
        }

        // 1. DANH SÁCH CẤU TRÚC
        public async Task<IActionResult> Index()
        {
            var items = await _context.ExamStructures
                .Include(s => s.Parts)
                .ToListAsync();
            return View(items);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExamStructure model)
        {
            if (ModelState.IsValid)
            {
                bool exists = await _context.ExamStructures.AnyAsync(s => s.Name == model.Name);
                if (exists)
                {
                    TempData["ErrorMessage"] = "Tên cấu trúc này đã tồn tại.";
                    return RedirectToAction(nameof(Index));
                }

             
                _context.ExamStructures.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã tạo cấu trúc " + model.Name;
              
                return RedirectToAction("Edit", new { id = model.Id });
            }

            TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại!";
            return RedirectToAction(nameof(Index));
        }

        // 2. EDIT (Sửa cấu trúc) - Thêm thông báo lỗi/thành công qua TempData
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var structure = await _context.ExamStructures
                .Include(s => s.Parts)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (structure == null) return NotFound();

            structure.Parts = structure.Parts.OrderBy(p => p.OrderIndex).ToList();
            return View(structure);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ExamStructure model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                // KIỂM TRA: Tên mới có trùng với cấu trúc KHÁC không?
                bool exists = await _context.ExamStructures.AnyAsync(s => s.Name == model.Name && s.Id != id);
                if (exists)
                {
                    ModelState.AddModelError("Name", "Tên cấu trúc này đã được sử dụng.");
                    // Load lại parts để view không bị lỗi
                    model.Parts = await _context.StructureParts.Where(p => p.ExamStructureId == id).OrderBy(p => p.OrderIndex).ToListAsync();
                    return View(model);
                }

                var existing = await _context.ExamStructures.FindAsync(id);
                existing.Name = model.Name;
                existing.Description = model.Description ?? "Không có";
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddPartAjax(int structureId, int orderIndex, string name, int skillType, string description)
        {
            // 1. VALIDATE DỮ LIỆU
            if (string.IsNullOrWhiteSpace(name))
                return Json(new { success = false, message = "Tên phần thi không được để trống!" });

            if (orderIndex <= 0)
                return Json(new { success = false, message = "Thứ tự phải lớn hơn 0!" });

            // 2. CHECK TRÙNG THỨ TỰ (Trong cùng cấu trúc)
            bool duplicateOrder = await _context.StructureParts
                .AnyAsync(p => p.ExamStructureId == structureId && p.OrderIndex == orderIndex);

            if (duplicateOrder)
                return Json(new { success = false, message = $"Thứ tự #{orderIndex} đã tồn tại!" });

            // 3. CHECK TRÙNG TÊN (Trong cùng cấu trúc)
            bool duplicateName = await _context.StructureParts
                .AnyAsync(p => p.ExamStructureId == structureId && p.Name == name.Trim());

            if (duplicateName)
                return Json(new { success = false, message = $"Tên phần thi '{name}' đã tồn tại!" });

            // 4. THÊM MỚI
            var part = new StructurePart
            {
                ExamStructureId = structureId,
                Name = name.Trim(),
                OrderIndex = orderIndex,
                SkillType = (ExamSystem.Core.Enums.ExamSkill)skillType,
                Description = description?.Trim() ?? ""
            };

            _context.StructureParts.Add(part);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Thêm thành công!" });
        }
        [HttpPost]
        public async Task<IActionResult> UpdatePartAjax(int partId, int orderIndex, string name, int skillType, string description)
        {
            // 1. Tìm bản ghi cần sửa TRƯỚC (để lấy ExamStructureId)
            var part = await _context.StructureParts.FindAsync(partId);

            if (part == null)
            {
                return Json(new { success = false, message = "Không tìm thấy dữ liệu!" });
            }

            // 2. Validate dữ liệu đầu vào
            if (string.IsNullOrWhiteSpace(name))
            {
                return Json(new { success = false, message = "Tên phần thi không được để trống!" });
            }
            if (orderIndex <= 0)
            {
                return Json(new { success = false, message = "Thứ tự phải lớn hơn 0!" });
            }
            // C. Kiểm tra trùng Tên (Name) trong cùng cấu trúc (Optional)
            bool duplicateName = await _context.StructureParts
                .AnyAsync(p => p.ExamStructureId == part.ExamStructureId
                          && p.Name == name
                          && p.Id != partId);

            if (duplicateName)
            {
                return Json(new { success = false, message = $"Tên #{orderIndex} đã được sử dụng bởi phần thi khác!" });
            }
            // 3. KIỂM TRA TRÙNG LẶP (Logic đã sửa)
            // Tìm xem trong CÙNG CẤU TRÚC CHA (part.ExamStructureId), có phần nào KHÁC (p.Id != partId)
            // mà đang giữ thứ tự này không.
            bool duplicateOrder = await _context.StructureParts
                .AnyAsync(p => p.ExamStructureId == part.ExamStructureId
                            && p.OrderIndex == orderIndex
                            && p.Id != partId); // Quan trọng: Loại trừ chính mình

            if (duplicateOrder)
            {
                // Gợi ý: Nếu trùng thì báo lỗi và UI sẽ phải tự reset lại số cũ
                return Json(new { success = false, message = $"Thứ tự #{orderIndex} đã được sử dụng bởi phần thi khác!" });
            }

            // 4. Cập nhật dữ liệu (Nếu mọi thứ OK)
            part.OrderIndex = orderIndex;
            part.Name = name.Trim();
            part.SkillType = (ExamSystem.Core.Enums.ExamSkill)skillType;
            part.Description = description?.Trim() ?? "";

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã lưu" });
        }

        [HttpPost]
        public async Task<IActionResult> DeletePart(int partId)
        {
            var part = await _context.StructureParts.FindAsync(partId);
            if (part != null)
            {
                int structureId = part.ExamStructureId;
                _context.StructureParts.Remove(part);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa phần thi.";
                return RedirectToAction(nameof(Edit), new { id = structureId });
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMultiple([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any())
            {
                return Json(new { success = false, message = "Không có cấu trúc nào được chọn." });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Lấy danh sách kèm theo Parts để EF xóa sạch các phần thi con (Cascade Delete)
                    var itemsToDelete = await _context.ExamStructures
                        .Include(s => s.Parts)
                        .Where(s => ids.Contains(s.Id))
                        .ToListAsync();

                    if (!itemsToDelete.Any())
                    {
                        return Json(new { success = false, message = "Không tìm thấy dữ liệu!" });
                    }

                    _context.ExamStructures.RemoveRange(itemsToDelete);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Json(new { success = true, message = $"Đã xóa thành công {itemsToDelete.Count} cấu trúc đề!" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var examStructure = await _context.ExamStructures
                .Include(e => e.Parts)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (examStructure == null)
            {
                return Json(new { success = false, message = "Không tìm thấy cấu trúc này!" });
            }

            try
            {
                // Khi xóa ExamStructure, EF Core sẽ tự động xóa các StructurePart bên trong
                _context.ExamStructures.Remove(examStructure);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GenerateStructureAjax(int structureId, int skillType)
        {
            var skill = (ExamSystem.Core.Enums.ExamSkill)skillType;

            // 1. Xác định thứ tự tiếp theo (để nối tiếp vào đuôi danh sách hiện tại)
            var lastOrder = await _context.StructureParts
                .Where(p => p.ExamStructureId == structureId)
                .MaxAsync(p => (int?)p.OrderIndex) ?? 0;

            var newParts = new List<StructurePart>();

            // 2. Định nghĩa các cấu trúc mẫu (Template)
            switch (skill)
            {
                case ExamSystem.Core.Enums.ExamSkill.Listening:
                    newParts.Add(new StructurePart { Name = "Listening Part 1: ", Description = "", SkillType = skill });
                    newParts.Add(new StructurePart { Name = "Listening Part 2: ", Description = "", SkillType = skill });
                    newParts.Add(new StructurePart { Name = "Listening Part 3: ", Description = "", SkillType = skill });                   
                    TempData["SuccessMessage"] = "Đã thêm 4 phần thi";
                    break;

                case ExamSystem.Core.Enums.ExamSkill.Reading:
                    newParts.Add(new StructurePart { Name = "Reading Part 4: ", Description = "", SkillType = skill });
                    newParts.Add(new StructurePart { Name = "Reading Part 5: ", Description = "", SkillType = skill });
                    newParts.Add(new StructurePart { Name = "Reading Part 6: ", Description = "", SkillType = skill });
                    newParts.Add(new StructurePart { Name = "Reading Part 7: ", Description = "", SkillType = skill });
                    TempData["SuccessMessage"] = "Đã thêm 4 phần thi"; 
                    break;

                case ExamSystem.Core.Enums.ExamSkill.Writing:
                    newParts.Add(new StructurePart { Name = "Writing Task 1", Description = "", SkillType = skill });
                    newParts.Add(new StructurePart { Name = "Writing Task 2", Description = "", SkillType = skill });
                    TempData["SuccessMessage"] = "Đã thêm 4 phần thi"; 
                    break;

                case ExamSystem.Core.Enums.ExamSkill.Speaking:
                    newParts.Add(new StructurePart { Name = "Speaking Part 1", Description = "", SkillType = skill });
                    newParts.Add(new StructurePart { Name = "Speaking Part 2", Description = "", SkillType = skill });
                    newParts.Add(new StructurePart { Name = "Speaking Part 3", Description = "", SkillType = skill });
                    TempData["SuccessMessage"] = "Đã thêm 4 phần thi"; 
                    break;

                default:
                    return Json(new { success = false, message = "Chưa có mẫu cho kỹ năng này!" });
            }

            // 3. Gán ID và Thứ tự, sau đó Lưu
            foreach (var part in newParts)
            {
                lastOrder++;
                part.ExamStructureId = structureId;
                part.OrderIndex = lastOrder;
            }

            _context.StructureParts.AddRange(newParts);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Tạo phần thi thành công" });
        }

        [HttpPost]
        public async Task<IActionResult> ClearStructureAjax(int structureId, int skillType)
        {
            var skill = (ExamSystem.Core.Enums.ExamSkill)skillType;

            // Tìm tất cả các part thuộc kỹ năng này trong cấu trúc này
            var partsToDelete = await _context.StructureParts
                .Where(p => p.ExamStructureId == structureId && p.SkillType == skill)
                .ToListAsync();

            if (!partsToDelete.Any())
            {
                return Json(new { success = false, message = "Không tồn tại phần thi cần xóa" });
            }

            _context.StructureParts.RemoveRange(partsToDelete);
            await _context.SaveChangesAsync();           
            return Json(new { success = true, message = "Đã xóa thành công" });
        }

    }
}