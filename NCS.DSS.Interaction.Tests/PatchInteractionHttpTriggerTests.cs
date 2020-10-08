﻿using Microsoft.Extensions.Logging;
using Moq;
using NCS.DSS.Interaction.Cosmos.Helper;
using NCS.DSS.Interaction.Helpers;
using NCS.DSS.Interaction.PatchInteractionHttpTrigger.Service;
using NCS.DSS.Interaction.Validation;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace NCS.DSS.Interaction.Tests
{
    [TestFixture]
    public class PatchInteractionHttpTriggerTests
    {
        private const string ValidCustomerId = "7E467BDB-213F-407A-B86A-1954053D3C24";
        private const string ValidInteractionId = "1e1a555c-9633-4e12-ab28-09ed60d51cb3";
        private const string InValidId = "1111111-2222-3333-4444-555555555555";
        private Mock<ILogger> _log;
        private HttpRequestMessage _request;
        private Mock<IResourceHelper> _resourceHelper;
        private IValidate _validate;
        private Mock<IHttpRequestMessageHelper> _httpRequestMessageHelper;
        private Mock<IPatchInteractionHttpTriggerService> _patchInteractionHttpTriggerService;
        private Models.Interaction _interaction;
        private Models.InteractionPatch _interactionPatch;
        private PatchInteractionHttpTrigger.Function.PatchInteractionHttpTrigger _function;

        [SetUp]
        public void Setup()
        {
            _interaction = new Models.Interaction();
            _interactionPatch = new Models.InteractionPatch();

            _request = new HttpRequestMessage()
            {
                Content = new StringContent(string.Empty),
                RequestUri =
                    new Uri($"http://localhost:7071/api/Customers/7E467BDB-213F-407A-B86A-1954053D3C24/Interactions/1e1a555c-9633-4e12-ab28-09ed60d51cb3")
            };

            _log = new Mock<ILogger>();
            _resourceHelper = new Mock<IResourceHelper>();
            _validate = new Validate();
            _httpRequestMessageHelper = new Mock<IHttpRequestMessageHelper>();
            _patchInteractionHttpTriggerService = new Mock<IPatchInteractionHttpTriggerService>();
            _function = new PatchInteractionHttpTrigger.Function.PatchInteractionHttpTrigger(_resourceHelper.Object, _httpRequestMessageHelper.Object, _patchInteractionHttpTriggerService.Object, _validate);
        }

        [Test]
        public async Task PatchInteractionHttpTrigger_ReturnsStatusCodeBadRequest_WhenTouchpointIdIsNotProvided()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetTouchpointId(_request)).Returns((string)null);

            // Act
            var result = await RunFunction(ValidCustomerId, ValidInteractionId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task PatchInteractionHttpTrigger_ReturnsStatusCodeBadRequest_WhenCustomerIdIsInvalid()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetApimURL(_request)).Returns("http://localhost:7071/");

            // Act
            var result = await RunFunction(InValidId, ValidInteractionId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task PatchInteractionHttpTrigger_ReturnsStatusCodeUnprocessableEntity_WhenInteractionHasFailedValidation()
        {
            // Arrange
            var validate = new Mock<IValidate>();
            var validationResults = new List<ValidationResult> { new ValidationResult("interaction Id is Required") };
            validate.Setup(x => x.ValidateResource(It.IsAny<Models.InteractionPatch>())).Returns(validationResults);
            _function = new PatchInteractionHttpTrigger.Function.PatchInteractionHttpTrigger(_resourceHelper.Object, _httpRequestMessageHelper.Object, _patchInteractionHttpTriggerService.Object, validate.Object);
            _httpRequestMessageHelper.Setup(x => x.GetTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetApimURL(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x => x.GetInteractionFromRequest<Models.InteractionPatch>(_request)).Returns(Task.FromResult(_interactionPatch));
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));

            // Act
            var result = await RunFunction(ValidCustomerId, ValidInteractionId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual((HttpStatusCode)422, result.StatusCode);
        }

        [Test]
        public async Task PatchInteractionHttpTrigger_ReturnsStatusCodeUnprocessableEntity_WhenInteractionRequestIsInvalid()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetApimURL(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x => x.GetInteractionFromRequest<Models.InteractionPatch>(_request)).Throws(new JsonException());

            // Act
            var result = await RunFunction(ValidCustomerId, ValidInteractionId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual((HttpStatusCode)422, result.StatusCode);
        }

        [Test]
        public async Task PatchInteractionHttpTrigger_ReturnsStatusCodeNoContent_WhenCustomerDoesNotExist()
        {
            //Arrange
            _httpRequestMessageHelper.Setup(x => x.GetTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetApimURL(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x => x.GetInteractionFromRequest<Models.InteractionPatch>(_request)).Returns(Task.FromResult(_interactionPatch));
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(false));

            // Act
            var result = await RunFunction(ValidCustomerId, ValidInteractionId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);
        }

        [Test]
        public async Task PatchInteractionHttpTrigger_ReturnsStatusCodeNoContent_WhenInteractionDoesNotExist()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetApimURL(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x => x.GetInteractionFromRequest<Models.InteractionPatch>(_request)).Returns(Task.FromResult(_interactionPatch));
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _patchInteractionHttpTriggerService.Setup(x => x.GetInteractionForCustomerAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.FromResult<Models.Interaction>(null));

            // Act
            var result = await RunFunction(ValidCustomerId, ValidInteractionId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);
        }

        [Test]
        public async Task PatchInteractionHttpTrigger_ReturnsStatusCodeBadRequest_WhenUnableToUpdateInteractionRecord()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetApimURL(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x => x.GetInteractionFromRequest<Models.InteractionPatch>(_request)).Returns(Task.FromResult(_interactionPatch));
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _patchInteractionHttpTriggerService.Setup(x => x.GetInteractionForCustomerAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.FromResult<Models.Interaction>(_interaction));
            _patchInteractionHttpTriggerService.Setup(x => x.UpdateAsync(It.IsAny<Models.Interaction>(), It.IsAny<Models.InteractionPatch>())).Returns(Task.FromResult<Models.Interaction>(null));

            // Act
            var result = await RunFunction(ValidCustomerId, ValidInteractionId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task PatchInteractionHttpTrigger_ReturnsStatusCodeOK_WhenRequestIsNotValid()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetApimURL(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x => x.GetInteractionFromRequest<Models.InteractionPatch>(_request)).Returns(Task.FromResult(_interactionPatch));
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _patchInteractionHttpTriggerService.Setup(x => x.GetInteractionForCustomerAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.FromResult<Models.Interaction>(_interaction));
            _patchInteractionHttpTriggerService.Setup(x => x.UpdateAsync(It.IsAny<Models.Interaction>(), It.IsAny<Models.InteractionPatch>())).Returns(Task.FromResult<Models.Interaction>(null));

            // Act
            var result = await RunFunction(ValidCustomerId, ValidInteractionId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task PatchInteractionHttpTrigger_ReturnsStatusCodeOK_WhenRequestIsValid()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetApimURL(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x => x.GetInteractionFromRequest<Models.InteractionPatch>(_request)).Returns(Task.FromResult(_interactionPatch));
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _patchInteractionHttpTriggerService.Setup(x => x.GetInteractionForCustomerAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.FromResult<Models.Interaction>(_interaction));
            _patchInteractionHttpTriggerService.Setup(x => x.UpdateAsync(It.IsAny<Models.Interaction>(), It.IsAny<Models.InteractionPatch>())).Returns(Task.FromResult<Models.Interaction>(_interaction));

            // Act
            var result = await RunFunction(ValidCustomerId, ValidInteractionId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        }

        private async Task<HttpResponseMessage> RunFunction(string customerId, string interactionId)
        {
            return await _function.Run(_request, _log.Object, customerId, interactionId).ConfigureAwait(false);
        }

    }
}