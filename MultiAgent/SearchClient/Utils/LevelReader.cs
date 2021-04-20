using System;

namespace MultiAgent.SearchClient.Utils
{
    public class LevelReader
    {
        private readonly Type _readerType;
        private int _fileCounter;
        private readonly string[] _fileBuffer;

        public LevelReader(Type readerType, string[] fileBuffer = null)
        {
            _readerType = readerType;
            _fileBuffer = fileBuffer;

            if (_readerType == Type.File && fileBuffer == null)
            {
                throw new ArgumentException("FileBuffer is needed when type if File");
            }
        }

        public string ReadLine()
        {
            return _readerType switch
            {
                Type.Console => Console.ReadLine(),
                Type.File => _fileBuffer[_fileCounter++],
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public enum Type
        {
            Console,
            File,
        }
    }
}
