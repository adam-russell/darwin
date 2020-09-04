// This file is part of DARWIN.
// Copyright (C) 1994 - 2020
//
// DARWIN is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// DARWIN is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with DARWIN.  If not, see<https://www.gnu.org/licenses/>.

using Darwin.ML.Model;
using Darwin.Utilities.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Darwin.ML.Services
{
    public class MLService
    {
        private const string ServiceBaseUrl = "https://www.photoidentification.net/";
        private const string ServiceUserAgent = "Darwin";


        private readonly HttpClient client = new HttpClient();

        public async Task<List<RawPoint>> GetRawContourAsync(Bitmap bitmap, string filename)
        {
            MemoryStream imageStream = new MemoryStream();
            bitmap.Save(imageStream, ImageFormat.Jpeg);
            imageStream.Position = 0;

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("User-Agent", ServiceUserAgent);

            Uri requestUri = new Uri(ServiceBaseUrl).Append("/api/images/findcontours");

            MultipartFormDataContent form = new MultipartFormDataContent();

            var speciesType = SpeciesType.BrownBear;

            HttpContent speciesTypeContent = new StringContent(speciesType.ToString());
            form.Add(speciesTypeContent, "SpeciesType");

            //HttpContent content = new StringContent("photos");
            //form.Add(content, "photos");

            var fileContent = new StreamContent(imageStream);
            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "File",
                FileName = Path.GetFileNameWithoutExtension(filename) + ".jpg"
            };
            form.Add(fileContent);

            client.Timeout = TimeSpan.FromMinutes(4);
            var streamResult = await client.PostAsync(requestUri, form);
            return await JsonSerializer.DeserializeAsync<List<RawPoint>>(await streamResult.Content.ReadAsStreamAsync());
        }
    }
}
