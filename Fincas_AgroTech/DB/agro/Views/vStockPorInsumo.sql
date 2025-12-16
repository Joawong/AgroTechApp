-- Vista de stock (por finca e insumo)
CREATE   VIEW agro.vStockPorInsumo AS
SELECT m.FincaId, m.InsumoId, SUM(m.Cantidad) AS Stock
FROM agro.MovimientoInventario m
GROUP BY m.FincaId, m.InsumoId;