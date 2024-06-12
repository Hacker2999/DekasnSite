using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using DekasnSite.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using OfficeOpenXml;

namespace DekasnSite.Controllers
{
    public class GradesController : Controller
    {
        private Dekan_dbEntities db = new Dekan_dbEntities();
        public class Export
        {
            public int ID_Grade { get; set; }
            public string StudentName { get; set; }
            public string TeacherName { get; set; }
            public string Discipline { get; set; }
            public int? Grade1 { get; set; }
            public DateTime? GradeDate { get; set; }
        }


        // Метод для экспорта данных в файл Excel
        public ActionResult ExportToExcel()
        {
            var applications = (from g in db.Grades
                                join s in db.Students on g.StudentID equals s.ID_Student
                                join t in db.Teachers on g.TeacherID equals t.ID_Teacher
                                select new Export
                                {
                                    ID_Grade = g.ID_Grade,
                                    StudentName = s.Name,
                                    TeacherName = t.Name,
                                    Discipline = g.Discipline,
                                    Grade1 = g.Grade1,
                                    GradeDate = g.GradeDate
                                }).ToList();


            // Задаем заголовки столбцов и свойства для включения в Excel
            var columnHeaders = new Dictionary<string, string>
    {
        { "ID_Grade", "ID Document" },
        { "StudentName", "Student Name" },
        { "TeacherName", "Teacher Name" },
        { "Discipline", "Discipline" },
        { "Grade1", "Grade" },
        { "GradeDate", "Grade Date" }
    };

            var propertiesToInclude = new List<string> { "ID_Grade", "StudentName", "TeacherName", "Discipline", "Grade1", "GradeDate" };

            // Создаем файл Excel и возвращаем его как результат действия
            var fileName = "applications.xlsx";
            return GenerateExcel(applications, fileName, columnHeaders, propertiesToInclude);
        }

        // Метод для экспорта данных в файл Word
        public ActionResult ExportToWord()
        {
            var applications = (from g in db.Grades
                                join s in db.Students on g.StudentID equals s.ID_Student
                                join t in db.Teachers on g.TeacherID equals t.ID_Teacher
                                select new Export
                                {
                                    ID_Grade = g.ID_Grade,
                                    StudentName = s.Name,
                                    TeacherName = t.Name,
                                    Discipline = g.Discipline,
                                    Grade1 = g.Grade1,
                                    GradeDate = g.GradeDate
                                }).ToList();


            // Задаем заголовки столбцов и свойства для включения в Word
            var columnHeaders = new Dictionary<string, string>
    {
        { "ID_Grade", "ID Document" },
        { "StudentName", "Student Name" },
        { "TeacherName", "Teacher Name" },
        { "Discipline", "Discipline" },
        { "Grade1", "Grade" },
        { "GradeDate", "Grade Date" }
    };

            var propertiesToInclude = new List<string> { "ID_Grade", "StudentName", "TeacherName", "Discipline", "Grade1", "GradeDate" };

            // Создаем файл Word и возвращаем его как результат действия
            var fileName = "applications.docx";
            return GenerateWord(applications, fileName, columnHeaders, propertiesToInclude);
        }


        // Метод для генерации файла Excel
        private ActionResult GenerateExcel(IEnumerable<Export> data, string fileName, Dictionary<string, string> columnHeaders, List<string> propertiesToInclude)
        {
            using (ExcelPackage package = new ExcelPackage())
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Applications");

                // Добавляем заголовки столбцов, используя переданный словарь
                int headerRow = 1;
                int column = 1;
                foreach (var columnHeader in columnHeaders)
                {
                    worksheet.Cells[headerRow, column].Value = columnHeader.Value;
                    column++;
                }

                // Добавляем данные из свойств, указанных в списке propertiesToInclude
                int dataRow = 2;
                foreach (var item in data)
                {
                    column = 1;
                    foreach (var property in propertiesToInclude)
                    {
                        var propValue = item.GetType().GetProperty(property)?.GetValue(item, null);
                        worksheet.Cells[dataRow, column].Value = propValue != null ? propValue.ToString() : "";
                        column++;
                    }
                    dataRow++;
                }

                // Форматируем таблицу
                worksheet.Cells.AutoFitColumns();
                worksheet.Cells[headerRow, 1, dataRow - 1, columnHeaders.Count].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                worksheet.Cells[headerRow, 1, headerRow, columnHeaders.Count].Style.Font.Bold = true;

                // Переводим данные в байтовый массив и возвращаем как файл
                byte[] fileBytes = package.GetAsByteArray();
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        // Метод для генерации файла Word
        private ActionResult GenerateWord(IEnumerable<Export> data, string fileName, Dictionary<string, string> columnHeaders, List<string> propertiesToInclude)
        {
            MemoryStream ms = new MemoryStream();
            using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
                Body body = mainPart.Document.AppendChild(new Body());

                // Добавляем таблицу
                Table table = new Table();
                TableProperties tableProperties = new TableProperties(
    new TableBorders(
        new TopBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
        new BottomBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
        new LeftBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
        new RightBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
        new InsideHorizontalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
        new InsideVerticalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 }
    )
);
                table.AppendChild(tableProperties);

                // Добавляем строку заголовков
                TableRow headerRow = new TableRow();
                foreach (var columnHeader in columnHeaders)
                {
                    TableCell headerCell = new TableCell(new Paragraph(new Run(new Text(columnHeader.Value))));

                    headerRow.AppendChild(headerCell);
                }
                table.AppendChild(headerRow);

                // Добавляем строки с данными
                foreach (var item in data)
                {
                    TableRow row = new TableRow();
                    foreach (var property in propertiesToInclude)
                    {
                        var prop = typeof(Export).GetProperty(property);
                        string value = prop.GetValue(item)?.ToString() ?? "";
                        TableCell cell = new TableCell(new Paragraph(new Run(new Text(value))));
                        row.AppendChild(cell);
                    }
                    table.AppendChild(row);
                }

                body.Append(table);

                // Сохраняем документ в поток
                wordDocument.Save();
            }

            // Возвращаем файл Word как результат действия
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
        }

        // GET: Grades
        public ActionResult Index(string sortOrder, string SearchString)
        {
            var grades = db.Grades.Include(g => g.Student).Include(g => g.Teacher);

            ViewBag.DisciplineParm = String.IsNullOrEmpty(sortOrder) ? "Discipline_desc" : "";
            ViewBag.GradeParm = sortOrder == "Grade" ? "Grade_desc" : "Grade";
            ViewBag.GradeDateParm = sortOrder == "GradeDate" ? "GradeDate_desc" : "GradeDate";
            ViewBag.StudentName = sortOrder == "StudentName" ? "StudentName_desc" : "StudentName";
            ViewBag.TeacherName = sortOrder == "TeacherName" ? "TeacherName_desc" : "TeacherName";

            grades = from a in db.Grades
                     select a;
            if (!String.IsNullOrEmpty(SearchString))
            {
                grades = grades.Where(s =>
                   s.Discipline.Contains(SearchString)
                || (s.Grade1 != null && s.Grade1.ToString().ToLower().Contains(SearchString.ToLower()))
                || s.Student.Name.Contains(SearchString)
                || s.Teacher.Name.Contains(SearchString));
            }
            switch (sortOrder)
            {
                case "Discipline_desc":
                    grades = grades.OrderByDescending(s => s.Discipline);
                    break;
                case "Grade":
                    grades = grades.OrderBy(s => s.Grade1);
                    break;
                case "Grade_desc":
                    grades = grades.OrderByDescending(s => s.Grade1);
                    break;
                case "GradeDate":
                    grades = grades.OrderBy(s => s.GradeDate);
                    break;
                case "GradeDate_desc":
                    grades = grades.OrderByDescending(s => s.GradeDate);
                    break;
                case "StudentName":
                    grades = grades.OrderBy(s => s.Student.Name);
                    break;
                case "StudentName_desc":
                    grades = grades.OrderByDescending(s => s.Student.Name);
                    break;
                case "TeacherName":
                    grades = grades.OrderBy(s => s.Teacher.Name);
                    break;
                case "TeacherName_desc":
                    grades = grades.OrderByDescending(s => s.Teacher.Name);
                    break;
                default:
                    grades = grades.OrderBy(s => s.Discipline);
                    break;
            }

            return View(grades.ToList());
        }

        // GET: Grades/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Grade grade = db.Grades.Find(id);
            if (grade == null)
            {
                return HttpNotFound();
            }
            return View(grade);
        }

        // GET: Grades/Create
        public ActionResult Create()
        {
            ViewBag.StudentID = new SelectList(db.Students, "ID_Student", "Name");
            ViewBag.TeacherID = new SelectList(db.Teachers, "ID_Teacher", "Name");
            return View();
        }

        // POST: Grades/Create
        // Чтобы защититься от атак чрезмерной передачи данных, включите определенные свойства, для которых следует установить привязку. Дополнительные 
        // сведения см. в разделе https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID_Grade,StudentID,TeacherID,Discipline,Grade1,GradeDate")] Grade grade)
        {
            if (ModelState.IsValid)
            {
                db.Grades.Add(grade);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.StudentID = new SelectList(db.Students, "ID_Student", "Name", grade.StudentID);
            ViewBag.TeacherID = new SelectList(db.Teachers, "ID_Teacher", "Name", grade.TeacherID);
            return View(grade);
        }

        // GET: Grades/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Grade grade = db.Grades.Find(id);
            if (grade == null)
            {
                return HttpNotFound();
            }
            ViewBag.StudentID = new SelectList(db.Students, "ID_Student", "Name", grade.StudentID);
            ViewBag.TeacherID = new SelectList(db.Teachers, "ID_Teacher", "Name", grade.TeacherID);
            return View(grade);
        }

        // POST: Grades/Edit/5
        // Чтобы защититься от атак чрезмерной передачи данных, включите определенные свойства, для которых следует установить привязку. Дополнительные 
        // сведения см. в разделе https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID_Grade,StudentID,TeacherID,Discipline,Grade1,GradeDate")] Grade grade)
        {
            if (ModelState.IsValid)
            {
                db.Entry(grade).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.StudentID = new SelectList(db.Students, "ID_Student", "Name", grade.StudentID);
            ViewBag.TeacherID = new SelectList(db.Teachers, "ID_Teacher", "Name", grade.TeacherID);
            return View(grade);
        }

        // GET: Grades/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Grade grade = db.Grades.Find(id);
            if (grade == null)
            {
                return HttpNotFound();
            }
            return View(grade);
        }

        // POST: Grades/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Grade grade = db.Grades.Find(id);
            db.Grades.Remove(grade);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
