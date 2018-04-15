using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sqllite
{
    public static class ReloadEntityHelper {
        public static void ReloadEntity<TEntity>(
        this DbContext context,
        TEntity entity)
        where TEntity : class
        {
            context.Entry(entity).Reload();
        }
    }
}
