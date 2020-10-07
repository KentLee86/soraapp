﻿using System;
using System.IO;
using bincombiner;

class FileInfos
{
    public enum CheckSum
    {
        CRC8,
        CRC16,
        CRC32
    }

    public FileInfo fi { get; set; }

    public string FileName
    {
        get => fi.Name;
    }

    public string FileSize
    {
        get => fi.Length.ToString("X08");
    }

    public long Offset;

    public string OffsetText
    {
        get
        {
            return Offset.ToString("X08");
        }
        set
        {
            Offset = Convert.ToInt64(value, 16);
        }
    }

    public UInt32 _crc;
    public string CRC
    {
        get
        {
            if (checksum == CheckSum.CRC8)
                return _crc.ToString("X02");
            else if (checksum == CheckSum.CRC16)
                return _crc.ToString("X04");
            else if (checksum == CheckSum.CRC32)
                return _crc.ToString("X08");
            else
                return _crc.ToString("X02");
        }
    }

    public CheckSum checksum;

    public void CalcChecksum()
    {
        if (fi.Exists)
        {
            var r = File.ReadAllBytes(fi.FullName);

            UInt32 crc = 0;

            foreach (var b in r)
            {
                crc += b;
            }

            if (checksum == CheckSum.CRC8)
            {
                crc &= 0xff;
            }
            else if (checksum == CheckSum.CRC16)
            {
                crc &= 0xffff;
            }
            else if (checksum == CheckSum.CRC32)
            {
                crc &= 0xffffffff;
            }

            _crc = crc;

        }
    }
}
