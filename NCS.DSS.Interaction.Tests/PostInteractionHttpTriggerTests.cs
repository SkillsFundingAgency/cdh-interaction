using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NCS.DSS.Interaction.Cosmos.Helper;
using NCS.DSS.Interaction.Helpers;
using NCS.DSS.Interaction.PostInteractionHttpTrigger.Service;
using NCS.DSS.Interaction.Validation;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace NCS.DSS.Interaction.Tests
{
    [TestFixture]
    public class PostInteractionHttpTriggerTests
    {
        private const string ValidCustomerId = "9DBE6C86-BC57-4CEB-88CB-000167897041";
        private const string InValidId = "1111111-2222-3333-4444-555555555555";
        private ILogger _log;
        private HttpRequestMessage _request;
        private IResourceHelper _resourceHelper;
        private IValidate _validate;
        private IHttpRequestMessageHelper _httpRequestMessageHelper;
        private IPostInteractionHttpTriggerService _postInteractionHttpTriggerService;
        private Models.Interaction _interaction;

        [SetUp]
        public void Setup()
        {
            _interaction = Substitute.For<Models.Interaction>();

            _request = new HttpRequestMessage()
            {
                Content = new StringContent(string.Empty),
                RequestUri = 
                    new Uri($"http://localhost:7071/api/Customers/7E467BDB-213F-407A-B86A-1954053D3C24/Interactions/")
            };

            _log = Substitute.For<ILogger>();
            _resourceHelper = Substitute.For<IResourceHelper>();
            _httpRequestMessageHelper = Substitute.For<IHttpRequestMessageHelper>();
            _validate = Substitute.For<IValidate>();
            _postInteractionHttpTriggerService = Substitute.For<IPostInteractionHttpTriggerService>();
            _httpRequestMessageHelper.GetTouchpointId(_request).Returns("0000000001");
            _httpRequestMessageHelper.GetApimURL(_request).Returns("http://localhost:7071/");
        }

        [Test]
        public async Task PostInteractionHttpTrigger_ReturnsStatusCodeBadRequest_WhenTouchpointIdIsNotProvided()
        {
            _httpRequestMessageHelper.GetTouchpointId(_request).Returns((string)null);

            // Act
            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task PostInteractionHttpTrigger_ReturnsStatusCodeBadRequest_WhenCustomerIdIsInvalid()
        {
            var result = await RunFunction(InValidId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task PostInteractionHttpTrigger_ReturnsStatusCodeUnprocessableEntity_WhenInteractionHasFailedValidation()
        {
            _httpRequestMessageHelper.GetInteractionFromRequest<Models.Interaction>(_request).Returns(Task.FromResult(_interaction).Result);

            var validationResults = new List<ValidationResult> { new ValidationResult("interaction Id is Required") };
            _validate.ValidateResource(Arg.Any<Models.Interaction>()).Returns(validationResults);

            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual((HttpStatusCode)422, result.StatusCode);
        }

        [Test]
        public async Task PostInteractionHttpTrigger_ReturnsStatusCodeUnprocessableEntity_WhenInteractionRequestIsInvalid()
        {
            _httpRequestMessageHelper.GetInteractionFromRequest<Models.InteractionPatch>(_request).Throws(new JsonException());

            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual((HttpStatusCode)422, result.StatusCode);
        }

        [Test]
        public async Task PostInteractionHttpTrigger_ReturnsStatusCodeNoContent_WhenCustomerDoesNotExist()
        {
            _httpRequestMessageHelper.GetInteractionFromRequest<Models.Interaction>(_request).Returns(Task.FromResult(_interaction).Result);

            _resourceHelper.DoesCustomerExist(Arg.Any<Guid>()).Returns(false);

            var result = await RunFunction(ValidCustomerId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);
        }

        //[Test]
        //public async Task PostInteractionHttpTrigger_ReturnsStatusCodeBadRequest_WhenUnableToCreateInteractionRecord()
        //{
        //    _httpRequestMessageHelper.GetInteractionFromRequest<Models.Interaction>(_request).Returns(Task.FromResult(_interaction).Result);

        //    _resourceHelper.DoesCustomerExist(Arg.Any<Guid>()).ReturnsForAnyArgs(true);

        //    _postInteractionHttpTriggerService.CreateAsync(Arg.Any<Models.Interaction>()).Returns(Task.FromResult<Models.Interaction>(null).Result);

        //    var result = await RunFunction(ValidCustomerId);

        //    // Assert
        //    Assert.IsInstanceOf<HttpResponseMessage>(result);
        //    Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        //}

        //[Test]
        //public async Task PostInteractionHttpTrigger_ReturnsStatusCodeCreated_WhenRequestIsNotValid()
        //{
        //    _httpRequestMessageHelper.GetInteractionFromRequest<Models.Interaction>(_request).Returns(Task.FromResult(_interaction).Result);

        //    _resourceHelper.DoesCustomerExist(Arg.Any<Guid>()).ReturnsForAnyArgs(true);

        //    _postInteractionHttpTriggerService.CreateAsync(Arg.Any<Models.Interaction>()).Returns(Task.FromResult<Models.Interaction>(null).Result);

        //    var result = await RunFunction(ValidCustomerId);
        //    result.ToString();
        //    // Assert
        //    Assert.IsInstanceOf<HttpResponseMessage>(result);
        //    Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        //}

        //[Test]
        //public async Task PostInteractionHttpTrigger_ReturnsStatusCodeCreated_WhenRequestIsValid()
        //{
        //    _httpRequestMessageHelper.GetInteractionFromRequest<Models.Interaction>(_request).Returns(Task.FromResult(_interaction).Result);

        //    _resourceHelper.DoesCustomerExist(Arg.Any<Guid>()).ReturnsForAnyArgs(true);

        //    _postInteractionHttpTriggerService.CreateAsync(Arg.Any<Models.Interaction>()).Returns(Task.FromResult<Models.Interaction>(_interaction).Result);

        //    var result = await RunFunction(ValidCustomerId);

        //    // Assert
        //    Assert.IsInstanceOf<HttpResponseMessage>(result);
        //    Assert.AreEqual(HttpStatusCode.Created, result.StatusCode);
        //}

        private async Task<HttpResponseMessage> RunFunction(string customerId)
        {
            return await PostInteractionHttpTrigger.Function.PostInteractionHttpTrigger.Run(
                _request, _log, customerId, _resourceHelper, _httpRequestMessageHelper, _validate, _postInteractionHttpTriggerService).ConfigureAwait(false);
        }

    }
}