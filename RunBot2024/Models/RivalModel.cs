using System.ComponentModel.DataAnnotations;

namespace RunBot2024.Models
{
    public class RivalModel
    {
        [Key]
        private int _id;
        private long _telegramId;
        private string _name;
        private string _company;
        private char _gender;
        private int _age;
        private double _totalResult;
        private DateTime _createdAt;
        private DateTime _updatedAt;

        public int Id { get { return _id; } set { _id = value; } }
        public long TelegramId { get { return _telegramId; } set { _telegramId = value; } }
        public string Name { get { return _name; } set { _name = value; } }
        public char Gender { get { return _gender; } set { _gender = value; } }
        public string Company { get { return _company; } set { _company = value; } }
        public int Age { get { return _age; } set { _age = value; } }
        public double TotalResult { get { return _totalResult; } set { _totalResult = value; } }
        public DateTime CreatedAt { get { return _createdAt; } set { _createdAt = value; } }
        public DateTime UpdatedAt { get { return _updatedAt; } set { _updatedAt = value; } }
    }
}
