﻿using System;
using System.Collections.Generic;
using System.Text;
using Domain.Entities;

namespace Domain.IRepositories
{
    public interface IGenreRepository
    {
        IEnumerable<Genre> GetAllGenres();
    }
}
