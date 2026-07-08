using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Format;

namespace LeakChecker.DataParser.Tests.Format
{
    public class CredentialAssignerTests
    {
        [Fact]
        public void Assign_ReturnsSame_WhenSchemaEmpty()
        {
            var schema = new Dictionary<int, ItemType>();

            var result = CredentialAssigner.Assign(schema);

            Assert.Same(schema, result);
        }

        [Fact]
        public void Assign_ReturnsSame_WhenUsernameAndPasswordExist()
        {
            var schema = new Dictionary<int, ItemType>
            {
                {0, ItemType.Username},
                {1, ItemType.Password},
                {2, ItemType.Other}
            };

            var result = CredentialAssigner.Assign(schema);

            Assert.Equal(schema, result);
        }

        [Fact]
        public void Assign_ReturnsSame_WhenTooManyOthers()
        {
            var schema = new Dictionary<int, ItemType>
            {
                {0, ItemType.Other},
                {1, ItemType.Other},
                {2, ItemType.Other},
                {3, ItemType.Other},
                {4, ItemType.Other}
            };

            var result = CredentialAssigner.Assign(schema);

            Assert.Equal(schema, result);
        }

        [Fact]
        public void Assign_Username_WhenIndexZeroIsOther()
        {
            var schema = new Dictionary<int, ItemType>
            {
                {0, ItemType.Other},
                {1, ItemType.Other}
            };

            var result = CredentialAssigner.Assign(schema);

            Assert.Equal(ItemType.Username, result[0]);
        }

        [Fact]
        public void Assign_Password_WhenOtherExistsAfterZero()
        {
            var schema = new Dictionary<int, ItemType>
            {
                {0, ItemType.Username},
                {1, ItemType.Other},
                {2, ItemType.Other}
            };

            var result = CredentialAssigner.Assign(schema);

            Assert.Equal(ItemType.Password, result[1]);
        }

        [Fact]
        public void Assign_UsernameAndPassword_WhenBothMissing()
        {
            var schema = new Dictionary<int, ItemType>
            {
                {0, ItemType.Other},
                {1, ItemType.Other}
            };

            var result = CredentialAssigner.Assign(schema);

            Assert.Equal(ItemType.Username, result[0]);
            Assert.Equal(ItemType.Password, result[1]);
        }

        [Fact]
        public void Assign_NotOverwriteNonOther()
        {
            var schema = new Dictionary<int, ItemType>
            {
                {0, ItemType.Other},
                {1, ItemType.Location}
            };

            var result = CredentialAssigner.Assign(schema);

            Assert.Equal(ItemType.Username, result[0]);
            Assert.Equal(ItemType.Location, result[1]); // unchanged
        }
    }
}
