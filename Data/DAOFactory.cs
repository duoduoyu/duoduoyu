using System;
using System.Collections.Generic;
using System.Text;

namespace Data
{
    public static class DAOFactory
    {
        public static T GetDAO<T>() where T : BaseDAO, new()
        {
            T dao = new T();
            return dao;
        }
    }
}
