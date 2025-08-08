using System.Collections.Generic;
using BlogComments.Functions.ModelBinding;
using BlogComments.Functions.Persistence;
using Shouldly;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BlogComments.Tests.Functions.ModelBinding
{
    [TestClass]
    public class ModelBinderTests
    {
        private readonly ModelBinder _binder;

        public ModelBinderTests()
        {
            _binder = new ModelBinder(new CommentContentsValidator());
        }

        [TestMethod]
        public void TryBindAndValidate_WhenEmptyForm_ReturnsFalse()
        {
            var form = new Dictionary<string, StringValues>
            {
            };

            var result = TryBindAndValidate(form, out _, out var _);

            result.ShouldBeFalse();
        }

        private bool TryBindAndValidate(
            Dictionary<string, StringValues> form,
            out CommentContents model,
            out IEnumerable<ValidationError> errors)
        {
            var collection = new FormCollection(form);

            return _binder.TryBindAndValidate(collection, out model, out errors);
        }
    }
}
