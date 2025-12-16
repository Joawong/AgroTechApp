CREATE   VIEW agro.vStockPorLote AS
SELECT m.FincaId, m.InsumoId, m.LoteId, SUM(m.Cantidad) AS Stock
FROM agro.MovimientoInventario m
GROUP BY m.FincaId, m.InsumoId, m.LoteId;
