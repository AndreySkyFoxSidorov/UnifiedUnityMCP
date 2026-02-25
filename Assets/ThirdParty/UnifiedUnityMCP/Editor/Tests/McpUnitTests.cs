using System;
using System.Collections.Generic;
using NUnit.Framework;
using SimpleJSON;
using Mcp.Editor.Commands;

namespace Mcp.Editor.Tests
{
    public class McpUnitTests
    {
        [Test]
        public void InitializeCommand_SetsListChangedToFalse()
        {
            var command = new InitializeCommand();
            JSONObject request = new JSONObject();
            request["id"] = 1;
            request["method"] = "initialize";

            JSONObject response = null;
            string error = null;

            command.Execute(request, 
                (res) => response = res, 
                (code, msg) => error = msg);

            Assert.IsNull(error, "Error should be null");
            Assert.IsNotNull(response, "Response should not be null");
            
            Assert.IsTrue(response.HasKey("capabilities"));
            Assert.IsTrue(response["capabilities"].AsObject.HasKey("tools"));
            Assert.IsTrue(response["capabilities"]["tools"].AsObject.HasKey("listChanged"));
            Assert.IsFalse(response["capabilities"]["tools"]["listChanged"].AsBool, "listChanged should be false");
        }

        [Test]
        public void ToolsListCommand_PaginationWorks()
        {
            var command = new ToolsListCommand();
            
            // Request 1: No cursor
            JSONObject request1 = new JSONObject();
            JSONObject response1 = null;
            command.Execute(request1, (res) => response1 = res, (c, m) => {});
            
            Assert.IsNotNull(response1);
            Assert.IsTrue(response1.HasKey("tools"));
            Assert.IsTrue(response1["tools"].AsArray.Count > 0);
            
            // Assume there are more than 20 tools initialized in total. We should get exactly 20 (the limit)
            if (response1.HasKey("nextCursor"))
            {
                Assert.AreEqual(20, response1["tools"].AsArray.Count, "Should return exactly 20 tools per page");
                
                string cursor = response1["nextCursor"].Value;
                Assert.AreEqual("20", cursor);

                // Request 2: With cursor
                JSONObject request2 = new JSONObject();
                JSONObject params2 = new JSONObject();
                params2["cursor"] = cursor;
                request2["params"] = params2;
                
                JSONObject response2 = null;
                command.Execute(request2, (res) => response2 = res, (c, m) => {});

                Assert.IsNotNull(response2);
                Assert.IsTrue(response2["tools"].AsArray.Count > 0);
                // The first tool on page 2 should be different from the first tool on page 1
                string firstToolPage1 = response1["tools"].AsArray[0]["name"].Value;
                string firstToolPage2 = response2["tools"].AsArray[0]["name"].Value;
                Assert.AreNotEqual(firstToolPage1, firstToolPage2);
            }
        }
    }
}
