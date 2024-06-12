using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
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
    public class ApplicationsController : Controller
    {
        public Dekan_dbEntities db = new Dekan_dbEntities();

        

        // Метод для экспорта данных в файл Excel
        public ActionResult ExportToExcel()
        {
            var applications = db.Applications.ToList();

            // Задаем заголовки столбцов и свойства для включения в Excel
            var columnHeaders = new Dictionary<string, string>
    {
        { "ID_Application", "ID_Application" },
        { "ApplicationType", "Application Type" },
        { "SubmissionDate", "Submission Date" },
        { "Status", "Status" }
    };

            var propertiesToInclude = new List<string> { "ID_Application", "ApplicationType", "SubmissionDate", "Status" };

            // Создаем файл Excel и возвращаем его как результат действия
            var fileName = "applications.xlsx";
            return GenerateExcel(applications, fileName, columnHeaders, propertiesToInclude);
        }


        // Метод для экспорта данных в файл Word
        public ActionResult ExportToWord()
        {
            var applications = db.Applications.ToList();

            // Задаем заголовки столбцов и свойства для включения в Word
            var columnHeaders = new Dictionary<string, string>
    {
        { "ID_Application", "ID_Application" },
        { "ApplicationType", "Application Type" },
        { "SubmissionDate", "Submission Date" },
        { "Status", "Status" }
    };

            var propertiesToInclude = new List<string> { "ID_Application", "ApplicationType", "SubmissionDate", "Status" };

            // Создаем файл Word и возвращаем его как результат действия
            var fileName = "applications.docx";
            return GenerateWord(applications, fileName, columnHeaders, propertiesToInclude);
        }


        // Метод для генерации файла Excel
        private ActionResult GenerateExcel(IEnumerable<Application> data, string fileName, Dictionary<string, string> columnHeaders, List<string> propertiesToInclude)
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
        private ActionResult GenerateWord(IEnumerable<Application> data, string fileName, Dictionary<string, string> columnHeaders, List<string> propertiesToInclude)
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
                        var prop = typeof(Application).GetProperty(property);
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



        // GET: Applications
        public ActionResult Index(string sortOrder, string searchString)
        {
            var application = db.Applications.Include(a => a.Applications);

            ViewBag.Application_TypeParm = String.IsNullOrEmpty(sortOrder) ? "application_type_desc" : "";
            ViewBag.SubmissionDateParm = sortOrder == "SubmissionDate" ? "SubmissionDate_desc" : "SubmissionDate";
            ViewBag.StatusParm = sortOrder == "Status" ? "Status_desc" : "Status";
            ViewBag.TeacherName = sortOrder == "TeacherName" ? "TeacherName_desc" : "TeacherName";

            application = from s in db.Applications
                          select s;

            if (!String.IsNullOrEmpty(searchString))
            {
                application = application.Where(s => s.ApplicationType.Contains(searchString)
                                                    || s.Status.Contains(searchString)
                                                    || s.Teacher.Name.Contains(searchString));
            }

            switch (sortOrder)
            {
                case "SubmissionDate":
                    application = application.OrderBy(s => s.SubmissionDate);
                    break;
                case "application_type_desc":
                    application = application.OrderByDescending(s => s.ApplicationType);
                    break;
                case "SubmissionDate_desc":
                    application = application.OrderByDescending(s => s.SubmissionDate);
                    break;
                case "Status":
                    application = application.OrderBy(s => s.Status);
                    break;
                case "Status_desc":
                    application = application.OrderByDescending(s => s.Status);
                    break;
                case "TeacherName":
                    application = application.OrderBy(s => s.Teacher.Name);
                    break;
                case "TeacherName_desc":
                    application = application.OrderByDescending(s => s.Teacher.Name);
                    break;
                default:
                    application = application.OrderBy(s => s.ApplicationType);
                    break;
            }

            return View("Index", application.ToList());
        }



        // GET: Applications/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Application application = db.Applications.Find(id);
            if (application == null)
            {
                return HttpNotFound();
            }
            return View(application);
        }

        // GET: Applications/Create
        public ActionResult Create()
        {
            ViewBag.ResponsibleTeacherID = new SelectList(db.Teachers, "ID_Teacher", "Name");
            return View();
        }

        // POST: Applications/Create
        // Чтобы защититься от атак чрезмерной передачи данных, включите определенные свойства, для которых следует установить привязку. Дополнительные 
        // сведения см. в разделе https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID_Application,ApplicationType,SubmissionDate,Status,ResponsibleTeacherID")] Application application)
        {
            if (ModelState.IsValid)
            {
                db.Applications.Add(application);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.ResponsibleTeacherID = new SelectList(db.Teachers, "ID_Teacher", "Name", application.ResponsibleTeacherID);
            return View(application);
        }

        // GET: Applications/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Application application = db.Applications.Find(id);
            if (application == null)
            {
                return HttpNotFound();
            }
            ViewBag.ResponsibleTeacherID = new SelectList(db.Teachers, "ID_Teacher", "Name", application.ResponsibleTeacherID);
            return View(application);
        }

        // POST: Applications/Edit/5
        // Чтобы защититься от атак чрезмерной передачи данных, включите определенные свойства, для которых следует установить привязку. Дополнительные 
        // сведения см. в разделе https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID_Application,ApplicationType,SubmissionDate,Status,ResponsibleTeacherID")] Application application)
        {
            if (ModelState.IsValid)
            {
                db.Entry(application).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.ResponsibleTeacherID = new SelectList(db.Teachers, "ID_Teacher", "Name", application.ResponsibleTeacherID);
            return View(application);
        }

        // GET: Applications/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Application application = db.Applications.Find(id);
            if (application == null)
            {
                return HttpNotFound();
            }
            return View(application);
        }

        // POST: Applications/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Application application = db.Applications.Find(id);
            db.Applications.Remove(application);
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
