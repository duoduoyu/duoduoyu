using System;
using System.Collections.Generic;
using Dappers;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Model;
using Model.Enum;
using System.Data;
using System.IO;
using Model.DTO;

namespace Data
{
    public class BaseDAO
    {
        private static IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        private static readonly IConfigurationRoot configuration = builder.Build();
        private static ThreadLocal<Dictionary<DB, ISqlSession>> sessions = new ThreadLocal<Dictionary<DB, ISqlSession>>(() => new Dictionary<DB, ISqlSession> { });
        private static string _connectionString;
        private readonly MySqlConnectionStrings _connStr;

        public interface IOptions<out TOptions> where TOptions : class, new()
        {
            TOptions Value { get; }
        }

        /// <summary>
        /// 获取字符串连接公共方法
        /// </summary>
        /// <returns></returns>
        private string GetConnectionString(string connKey)
        {
            string connectionStr = configuration["MySqlConnectionStrings:" + connKey];
            return connectionStr;
        }

        /// <summary>
        /// 获取主库连接地址
        /// </summary>
        /// <returns></returns>
        private string GetConnectionString()
        {
            if (String.IsNullOrEmpty(_connectionString))
            {
                _connectionString = this.GetConnectionString("Context");
            }
            return _connectionString;
        }

        public ISqlSession GetSession(DB dbType)
        {
            Dappers.Query.Dao dao = null;
            if (!sessions.Value.ContainsKey(dbType))
            {
                lock (this)
                {
                    if (!sessions.Value.ContainsKey(dbType))
                    {
                        switch (dbType)
                        {
                            case DB.Master:
                                dao = new Dappers.Query.Dao(GetConnectionString(), "MySql.Data.MySqlClient.MySqlConnection, MySql.Data.Core, Version=7.0.4.0, Culture=neutral, PublicKeyToken=c5687fc88969c44d");
                                break;
                            default:
                                throw new Exception(string.Format("不支持该数据库：{0}！", dbType));
                        }
                        sessions.Value.Add(dbType, new Dappers.Session.DapperSession(dao));
                    }
                }
            }
            return sessions.Value[dbType];
        }

        public ISqlSession dbPublic
        {
            get
            {
                return GetSession(DB.Master);
            }
        }

        public string GetInParameter(string inputs, bool isInt, ref IDictionary<string, object> parameters, string arg = "InArg")
        {
            var inputList = inputs.Split(',');
            var paramSql = string.Empty;
            for (int i = 0; i < inputList.Length; i++)
            {
                if (isInt)
                {
                    //if (!SIFunction.IsNumberic(inputList[i].Trim()))
                    //{
                    //    continue;
                    //}
                    paramSql += string.Format("@{0}{1},", arg, i);
                    parameters.Add(string.Format("@{0}{1}", arg, i), inputList[i]);
                }
                else
                {
                    paramSql += string.Format("@{0}{1},", arg, i);
                    parameters.Add(string.Format("@{0}{1}", arg, i), string.Format("{0}", inputList[i]));
                }
            }
            return paramSql.Trim(',');
        }

        public static void BeginTran(DB dbType)
        {
            switch (dbType)
            {
                case DB.Master:
                    ISqlSession openSession;
                    if (sessions.Value.ContainsKey(DB.Master))
                    {
                        openSession = sessions.Value[DB.Master];
                    }
                    else
                    {
                        openSession = new BaseDAO().GetSession(DB.Master);
                    }
                    openSession.TxBegin();
                    break;
                default:
                    throw new Exception(string.Format("不支持该数据库：{0}！", dbType));
            }
        }

        public static void CommitTran(DB dbType)
        {
            switch (dbType)
            {
                case DB.Master:
                    ISqlSession openSession;
                    if (sessions.Value.ContainsKey(DB.Master))
                    {
                        openSession = sessions.Value[DB.Master];
                    }
                    else
                    {
                        openSession = new BaseDAO().GetSession(DB.Master);
                    }
                    openSession.TxCommit();
                    break;
                default:
                    throw new Exception(string.Format("不支持该数据库：{0}！", dbType));
            }
        }

        public static void RollbackTran(DB dbType)
        {

            switch (dbType)
            {
                case DB.Master:
                    ISqlSession openSession;
                    if (sessions.Value.ContainsKey(DB.Master))
                    {
                        openSession = sessions.Value[DB.Master];
                    }
                    else
                    {
                        openSession = new BaseDAO().GetSession(DB.Master);
                    }
                    openSession.TxRollback();
                    break;
                default:
                    throw new Exception(string.Format("不支持该数据库：{0}！", dbType));
            }
        }

        public static void CloseSession()
        {
            if (sessions.Value.ContainsKey(DB.Master))
            {
                sessions.Value[DB.Master].Close();
            }
        }
    }
}
