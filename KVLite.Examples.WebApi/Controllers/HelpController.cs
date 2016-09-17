// File name: HelpController.cs
// 
// Author(s): Alessio Parma <alessio.parma@gmail.com>
// 
// The MIT License (MIT)
// 
// Copyright (c) 2014-2016 Alessio Parma <alessio.parma@gmail.com>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Web.Http;

namespace RestService.WebApi.Controllers
{
    /// <summary>
    ///   The HELP controller.
    /// </summary>
    /// <remarks>Adjust routing prefix according to your own needs.</remarks>
    [RoutePrefix("")]
    public sealed class HelpController : ApiController
    {
        /// <summary>
        ///   Redirects to Swagger help pages.
        /// </summary>
        [Route("")]
        public IHttpActionResult Get()
        {
            var uri = Request.RequestUri.ToString();
            var uriWithoutQuery = uri.Substring(0, uri.Length - Request.RequestUri.Query.Length);
            return Redirect(uriWithoutQuery + "swagger");
        }
    }
}
