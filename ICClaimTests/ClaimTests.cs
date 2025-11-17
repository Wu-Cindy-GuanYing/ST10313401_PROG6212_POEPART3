using Xunit;
using ContractMonthlyClaimSystem.Models;
namespace ICClaimTests
{
    public class ClaimTests
    {
        [Fact]
        public void CalculateTotalAmount()
        {
            var claim = new ClaimItem();
            claim.Hours = 20;
            claim.Rate = 670;

            var getResult = claim.CalculateTotalAmount();

            Assert.Equal(13400, getResult);
        }
        [Fact]
        public void AdditionalNotes_Simulation()
        {
            var claim = new ClaimItem();
            claim.Description = "This is a test note for the claim Description.";

            var description = claim.Description;
            Assert.Equal("This is a test note for the claim Description.", description);
        }
        [Fact]
        public void FileProperties_IsStoredCorrectly()
        {
            var claim = new Document();
            claim.FileName = "invoice.pdf";
            claim.ContentType = "pdf";

            Assert.Equal("invoice.pdf", claim.FileName);
            Assert.Equal("pdf", claim.ContentType);
        }



    }
}
