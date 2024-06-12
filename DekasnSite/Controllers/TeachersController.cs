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
using DekasnSite.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using OfficeOpenXml;

namespace DekasnSite.Controllers
{
    public class TeachersController : Controller
    {
        private Dekan_dbEntities db = new Dekan_dbEntities();

        // Метод для экспорта данных в файл Excel
        public ActionResult ExportToExcel()
        {
            var applications = db.Teachers.ToList();

            // Задаем заголовки столбцов и свойства для включения в Excel
            var columnHeaders = new Dictionary<string, string>
    {
        { "ID_Teacher", "ID Teacher" },
        { "Name", "Name" },
        { "Surname", "Surname" },
        { "Department", "Department" },
        { "Email", "Eamil" },
        { "ContactNumber", "Contact Number" },

    };

            var propertiesToInclude = new List<string> { "ID_Teacher", "Name", "Surname", "Department", "Email", "ContactNumber" };

            // Создаем файл Excel и возвращаем его как результат действия
            var fileName = "applications.xlsx";
            return GenerateExcel(applications, fileName, columnHeaders, propertiesToInclude);
        }


        // Метод для экспорта данных в файл Word
        public ActionResult ExportToWord()
        {
            var applications = db.Teachers.ToList();

            // Задаем заголовки столбцов и свойства для включения в Word
            var columnHeaders = new Dictionary<string, string>
    {
       { "ID_Teacher", "ID Teacher" },
        { "Name", "Name" },
        { "Surname", "Surname" },
        { "Department", "Department" },
        { "Email", "Eamil" },
        { "ContactNumber", "Contact Number" },
    };

            var propertiesToInclude = new List<string> { "ID_Teacher", "Name", "Surname", "Department", "Email", "ContactNumber" };

            // Создаем файл Word и возвращаем его как результат действия
            var fileName = "applications.docx";
            return GenerateWord(applications, fileName, columnHeaders, propertiesToInclude);
        }


        // Метод для генерации файла Excel
        private ActionResult GenerateExcel(IEnumerable<Teacher> data, string fileName, Dictionary<string, string> columnHeaders, List<string> propertiesToInclude)
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
        private ActionResult GenerateWord(IEnumerable<Teacher> data, string fileName, Dictionary<string, string> columnHeaders, List<string> propertiesToInclude)
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
                        var prop = typeof(Teacher).GetProperty(property);
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


        // GET: Teachers
        public ActionResult Index(string sortOrder, string SearchString)
        {
            ViewBag.NameParm = String.IsNullOrEmpty(sortOrder) ? "Name_desc" : "";
            ViewBag.SurnameParm = sortOrder == "Surname" ? "Surname_desc" : "Surname";
            ViewBag.DepartmentParm = sortOrder == "Department" ? "Department_desc" : "Department";
            ViewBag.ContactNumberParm = sortOrder == "ContactNumber" ? "ContactNumber_desc" : "ContactNumber";
            ViewBag.EmailParm = sortOrder == "Email" ? "Email_desc" : "Email";


            var teachers = from a in db.Teachers
                           select a;
            if (!String.IsNullOrEmpty(SearchString))
            {
                teachers = teachers.Where
                    (s => s.Name.Contains(SearchString)
                    || s.Surname.Equals(SearchString)
                    || s.Department.Contains(SearchString)
                    || s.ContactNumber.Contains(SearchString)
                    || s.Email.Contains(SearchString));

            }
            switch (sortOrder)
            {
                case "Name_desc":
                    teachers = teachers.OrderByDescending(s => s.Name);
                    break;
                case "Surname":
                    teachers = teachers.OrderBy(s => s.Surname);
                    break;
                case "Surname_desc":
                    teachers = teachers.OrderByDescending(s => s.Surname);
                    break;
                case "Department":
                    teachers = teachers.OrderBy(s => s.Department);
                    break;
                case "Department_desc":
                    teachers = teachers.OrderByDescending(s => s.Department);
                    break;
                case "ContactNumber":
                    teachers = teachers.OrderBy(s => s.ContactNumber);
                    break;
                case "ContactNumber_desc":
                    teachers = teachers.OrderByDescending(s => s.ContactNumber);
                    break;
                case "Email":
                    teachers = teachers.OrderBy(s => s.Email);
                    break;
                case "Email_desc":
                    teachers = teachers.OrderByDescending(s => s.Email);
                    break;
                default:
                    teachers = teachers.OrderBy(s => s.Name);
                    break;
            }

            return View(teachers.ToList());
        }

        // GET: Teachers/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Teacher teacher = db.Teachers.Find(id);
            if (teacher == null)
            {
                return HttpNotFound();
            }
            return View(teacher);
        }

        // GET: Teachers/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Teachers/Create
        // Чтобы защититься от атак чрезмерной передачи данных, включите определенные свойства, для которых следует установить привязку. Дополнительные 
        // сведения см. в разделе https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID_Teacher,Name,Surname,Department,ContactNumber,Email")] Teacher teacher)
        {
            if (ModelState.IsValid)
            {
                db.Teachers.Add(teacher);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(teacher);
        }

        // GET: Teachers/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Teacher teacher = db.Teachers.Find(id);
            if (teacher == null)
            {
                return HttpNotFound();
            }
            return View(teacher);
        }

        // POST: Teachers/Edit/5
        // Чтобы защититься от атак чрезмерной передачи данных, включите определенные свойства, для которых следует установить привязку. Дополнительные 
        // сведения см. в разделе https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID_Teacher,Name,Surname,Department,ContactNumber,Email")] Teacher teacher, HttpPostedFileBase upload)
        {
            if (ModelState.IsValid)
            {
                db.Entry(teacher).State = EntityState.Modified;
                if (upload != null && upload.ContentLength > 0)
                {
                    using (var reader = new System.IO.BinaryReader(upload.InputStream))
                    {
                        teacher.Photo = reader.ReadBytes(upload.ContentLength);
                    }
                    db.SaveChanges();
                }

                else
                {
                    db.Entry(teacher).Property(m => m.Photo).IsModified = false;
                    db.SaveChanges();
                }

                return RedirectToAction("Index");
            }

            return View(teacher);
        }

        // GET: Teachers/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Teacher teacher = db.Teachers.Find(id);
            if (teacher == null)
            {
                return HttpNotFound();
            }
            return View(teacher);
        }

        // POST: Teachers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Teacher teacher = db.Teachers.Find(id);
            db.Teachers.Remove(teacher);
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
