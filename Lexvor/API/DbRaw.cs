using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Lexvor.API
{
    public static class DbRaw
    {
		public static IDbConnection NewConnection(string cStr) {
			try {
				var connection = new SqlConnection(cStr);
				connection.Open();
				return connection;
			}
			catch (NullReferenceException ex) {
				throw new Exception("Null reference exception. Most likely a missing connection string.", ex);
			}
			catch (Exception ex) {
				throw new Exception("Something bad happened.", ex);
			}
		}

		public static int Execute(string sql, object parameters, string cStr) {
			using (var connection = NewConnection(cStr)) {
				return connection.Execute(sql, parameters);
			}
		}

		public static IEnumerable<T> Query<T>(string sql, object parameters, string cStr) {
			using (var conn = NewConnection(cStr)) {
				return conn.Query<T>(sql, parameters);
			}
		}

		public static IEnumerable<T> GetListPaged<T>(int pageNumber, int perPage, string where, string orderby, object parameters, string cStr) {
			using (var conn = NewConnection(cStr)) {
				return conn.GetListPaged<T>(pageNumber, perPage, where, orderby, parameters);
			}
		}

		public static int GetRecordCount<T>(string where, object parameters, string cStr) {
			using (var conn = NewConnection(cStr)) {
				return conn.RecordCount<T>(where, parameters);
			}
		}

		public static T First<T>(string sql, object parameters, string cStr) {
			return Query<T>(sql, parameters, cStr).FirstOrDefault();
		}

		public static Guid Insert<T>(T obj, string cStr) where T : class {
			using (var connection = NewConnection(cStr)) {
			    return connection.Insert<Guid, T>(obj);
			}
		}

		public static bool Update<T>(T obj, string cStr) where T : class {
			using (var connection = NewConnection(cStr)) {
				return connection.Update(obj) == 1;
			}
		}

		public static T Get<T>(Guid id, string cStr) where T : class {
			using (var connection = NewConnection(cStr)) {
				return connection.Get<T>(id);
			}
		}
	}
}
