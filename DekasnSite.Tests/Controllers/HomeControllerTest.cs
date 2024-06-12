using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using DekasnSite.Controllers;
using DekasnSite.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace DekasnSite.Tests.Controllers
{
    [TestClass]
    public class ApplicationsControllerTest
    {
        private Mock<Dekan_dbEntities> mockDbContext;
        private ApplicationsController controller;

        [TestInitialize]
        public void Setup()
        {
            mockDbContext = new Mock<Dekan_dbEntities>();
            controller = new ApplicationsController { db = mockDbContext.Object };

        }

        [TestMethod]
        public void CreatePostMethodAddsApplicationAndRedirects()
        {
            // Arrange
            var application = new Application { ID_Application = 1, ApplicationType = "Type 1", SubmissionDate = DateTime.Now, Status = "Pending" };
            mockDbContext.Setup(m => m.Applications.Add(It.IsAny<Application>()));
            mockDbContext.Setup(m => m.SaveChanges());

            // Act
            var result = controller.Create(application) as RedirectToRouteResult;

            // Assert
            mockDbContext.Verify(m => m.Applications.Add(It.IsAny<Application>()), Times.Once);
            mockDbContext.Verify(m => m.SaveChanges(), Times.Once);
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.RouteValues["action"]);
        }

        [TestMethod]
        public void DeletePostMethodRemovesApplicationAndRedirects()
        {
            // Arrange
            var application = new Application { ID_Application = 1, ApplicationType = "Type 1", SubmissionDate = DateTime.Now, Status = "Pending" };
            mockDbContext.Setup(m => m.Applications.Find(It.IsAny<int>())).Returns(application);
            mockDbContext.Setup(m => m.Applications.Remove(It.IsAny<Application>()));
            mockDbContext.Setup(m => m.SaveChanges());

            // Act
            var result = controller.DeleteConfirmed(1) as RedirectToRouteResult;

            // Assert
            mockDbContext.Verify(m => m.Applications.Remove(It.IsAny<Application>()), Times.Once);
            mockDbContext.Verify(m => m.SaveChanges(), Times.Once);
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.RouteValues["action"]);
        }



    }
}
