using System;
using System.IO;
using System.Linq;
using System.Text;
using MimeKit;
using Npgsql;

namespace Speculatores
{
    public class MailQueue
    {
        private readonly MessageBuffer _buffer;

        public MailQueue(NpgsqlConnection db, string facilityName)
        {
            _buffer = new MessageBuffer(db, "mailqueue", facilityName);
        }

        private string MailToString(MimeMessage message)
        {
            using (var stream = new MemoryStream())
            {
                message.WriteTo(stream);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private MimeMessage MailFromString(string text)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
            {
                return MimeMessage.Load(stream);
            }
        }

        public bool Any()
        {
            return _buffer.Count() > 0;
        }

        public long Count()
        {
            return _buffer.Count();
        }

        public void Enqueue(MimeMessage message)
        {
            var text = MailToString(message);
            var id = Util.CreateId(text);
            if (!_buffer.Contains(id))
            {
                _buffer.Insert(new Message(id, text));
            }
        }

        public MimeMessage Dequeue()
        {
            var message = _buffer.Select(1).Single();
            _buffer.Delete(message.Id);
            return MailFromString(message.Text);
        }
    }
}
