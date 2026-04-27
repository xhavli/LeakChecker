using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Format;

namespace LeakChecker.DataParser.Tests.Format
{
    public class CredentialAssignerTests
    {
        [Fact]
        public void Assign_ReturnsSame_WhenSchemaEmpty()
        {
            var schema = new Dictionary<int, ItemEnum>();

            var result = CredentialAssigner.Assign(schema);

            Assert.Same(schema, result);
        }

        [Fact]
        public void Assign_ReturnsSame_WhenUsernameAndPasswordExist()
        {
            var schema = new Dictionary<int, ItemEnum>
            {
                {0, ItemEnum.Username},
                {1, ItemEnum.Password},
                {2, ItemEnum.Other}
            };

            var result = CredentialAssigner.Assign(schema);

            Assert.Equal(schema, result);
        }

        [Fact]
        public void Assign_ReturnsSame_WhenTooManyOthers()
        {
            var schema = new Dictionary<int, ItemEnum>
            {
                {0, ItemEnum.Other},
                {1, ItemEnum.Other},
                {2, ItemEnum.Other},
                {3, ItemEnum.Other},
                {4, ItemEnum.Other}
            };

            var result = CredentialAssigner.Assign(schema);

            Assert.Equal(schema, result);
        }

        [Fact]
        public void Assign_Username_WhenIndexZeroIsOther()
        {
            var schema = new Dictionary<int, ItemEnum>
            {
                {0, ItemEnum.Other},
                {1, ItemEnum.Other}
            };

            var result = CredentialAssigner.Assign(schema);

            Assert.Equal(ItemEnum.Username, result[0]);
        }

        [Fact]
        public void Assign_Password_WhenOtherExistsAfterZero()
        {
            var schema = new Dictionary<int, ItemEnum>
            {
                {0, ItemEnum.Username},
                {1, ItemEnum.Other},
                {2, ItemEnum.Other}
            };

            var result = CredentialAssigner.Assign(schema);

            Assert.Equal(ItemEnum.Password, result[1]);
        }

        [Fact]
        public void Assign_UsernameAndPassword_WhenBothMissing()
        {
            var schema = new Dictionary<int, ItemEnum>
            {
                {0, ItemEnum.Other},
                {1, ItemEnum.Other}
            };

            var result = CredentialAssigner.Assign(schema);

            Assert.Equal(ItemEnum.Username, result[0]);
            Assert.Equal(ItemEnum.Password, result[1]);
        }

        [Fact]
        public void Assign_NotOverwriteNonOther()
        {
            var schema = new Dictionary<int, ItemEnum>
            {
                {0, ItemEnum.Other},
                {1, ItemEnum.Location}
            };

            var result = CredentialAssigner.Assign(schema);

            Assert.Equal(ItemEnum.Username, result[0]);
            Assert.Equal(ItemEnum.Location, result[1]); // unchanged
        }
    }
}
