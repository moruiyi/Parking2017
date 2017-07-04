﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace Parking.Data
{
    public class RemoteFeeRecordManager:BaseManager<RemotePayFeeRcd>
    {
        public RemotePayFeeRcd Find(Expression<Func<RemotePayFeeRcd, bool>> where)
        {
            return _repository.Find(where);
        }
    }
}