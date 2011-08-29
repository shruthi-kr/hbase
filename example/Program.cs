using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HBase.Thrift;
using Thrift.Protocol;
using Thrift.Transport;

namespace HBase.Example
{
    class Program
    {
        private static Hbase.Client _hbase;
        static byte[] table_name = Encoding.UTF8.GetBytes("vadim_test");
        static readonly byte[] ID = Encoding.UTF8.GetBytes("Id");
        static readonly byte[] NAME = Encoding.UTF8.GetBytes("Name");
        static int i = 0;

        static void Main(string[] args)
        {
            int port = 9090;

            string host = args.Length == 1 ? args[0] : "localhost";

            var socket = new TSocket(host, port);
            var transport = new TBufferedTransport(socket);
            var proto = new TBinaryProtocol(transport);
            _hbase = new Hbase.Client(proto);

            try
            {
                transport.Open();

                var names = _hbase.getTableNames();
                names.ForEach(msg => Console.WriteLine(Encoding.UTF8.GetString(msg)));

                CreateTable();
                Insert();
                Get();

                transport.Close();
            } catch(Exception e)
            {
                Console.Error.Write(e.Message);
            }
        }

        private static void Get()
        {
            var scanner = _hbase.scannerOpen(table_name, Guid.Empty.ToByteArray(),
                                             new List<byte[]>(){ID});
            for (var entry = _hbase.scannerGet(scanner); entry.Count > 0; entry = _hbase.scannerGet(scanner))
            {
                foreach (var rowResult in entry)
                {
                    Console.Write("{0} => ", new Guid(rowResult.Row));
                    var res = rowResult.Columns.Select(c => BitConverter.ToInt32(c.Value.Value, 0));
                    foreach (var cell in res)
                    {
                        Console.WriteLine("{0}", cell);
                    }
                }
            }
        }

        private static void Insert()
        {
            _hbase.mutateRows(table_name, new List<BatchMutation>()
            {
                new BatchMutation()
                {
                    Row = Guid.NewGuid().ToByteArray(),
                    Mutations = new List<Mutation> {
                        new Mutation{Column = ID, IsDelete = false, Value = BitConverter.GetBytes(i++) }
                    }
                },
                new BatchMutation()
                {
                    Row = Guid.NewGuid().ToByteArray(),
                    Mutations = new List<Mutation> {
                        new Mutation{Column = ID, IsDelete = false, Value = BitConverter.GetBytes(i++) }
                    }
                }
            });
        }

        private static void CreateTable()
        {
            _hbase.disableTable(table_name);
            _hbase.deleteTable(table_name);

            _hbase.createTable(
                table_name,
                new List<ColumnDescriptor>()
                    {
                        new ColumnDescriptor {Name = ID, InMemory = true},
                        new ColumnDescriptor {Name = NAME, InMemory = true}
                    }
                );
        }
    }
}
