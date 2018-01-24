namespace AngelsChat.Shared.Data
{
    public class UserDto
    {
        public string Name { get; set; }

        public UserDto() { }

        public UserDto(string name)
        {
            Name = name;
        }
    }
}
