using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace GoodlyFere.Import.Ektron.Tests
{
    public class MetadataDestinationTests
    {
        [Fact]
        public void NoContentId_Throws()
        {
            var dest = new MetadataDestination();

        }
    }
}
