using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace RunBot2024.Models
{
    public class ErrorLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        private int _id;
        private string _errorMessage;
        private long _telegramId;
        private DateTime _lastUpdated;

        public int Id { get => _id; set => _id = value; }
        public string ErrorMessage { get => _errorMessage; set => _errorMessage = value; }
        public long TelegramId { get => _telegramId; set => _telegramId = value; }
        public DateTime LastUpdated { get => _lastUpdated; set => _lastUpdated = value; }
    }
}
