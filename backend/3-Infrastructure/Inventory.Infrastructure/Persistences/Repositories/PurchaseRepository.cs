using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain;

namespace Inventory.Infrastructure;

public class PurchaseRepository(IPurchaseDetailRepository _purchaseDetailRepository, InventoryDbContext _DbContext):IPurchaseRepository
{
    public async Task<bool> CreatePurchase(Purchase purchase)
    {
        using var db = _DbContext.CreateConnection;
        bool ok;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                purchase.Id = Guid.NewGuid();
                string sqlQuery = @"
                        INSERT INTO purchases
                             (id,  purchase_date,  total, is_active,  state, created_by,  created,  modified_by, modified, provider_id, estimated_delivery_date, purchase_status_id)
                       VALUES(@Id, @PurchaseDate, @Total, @IsActive, @State, @CreatedBy,  @Created, @ModifiedBy, @Modified, @ProviderId, @EstimatedDeliveryDate, @PurchaseStatusId)      ";

                purchase.PurchaseStatusId = (int)Domain.Enums.PurchaseStatusEnum.REQUESTED;
                var result = await db.ExecuteAsync(sqlQuery, purchase);

                purchase.Detail.ForEach(x => { x.PurchaseId = purchase.Id; x.PurchaseStatusId = purchase.PurchaseStatusId; });

                foreach (var detail in purchase.Detail)
                {
                    detail.State = true;
                    await _purchaseDetailRepository.CreatePurchaseDetail(detail, db, transaction);
                }
                transaction.Commit();
                ok = true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        finally
        {
            db.Close();
        }
        return ok;
    }


    public async Task<int> UpdatePurchase(Purchase purchase)
    {
        using var db = _DbContext.CreateConnection;
        int numberRows = 0;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                string sqlQuery = @"
                         UPDATE purchases
                           SET purchase_date= @PurchaseDate, 
                               total= @Total, 
                               is_active= @IsActive, 
                               modified_by= @ModifiedBy, 
                               modified= @Modified,
                               purchase_status_id = @PurchaseStatusId
                         WHERE id= @Id;
                    ";
                numberRows = await db.ExecuteAsync(sqlQuery, purchase);
                purchase.Detail.ForEach(x => { x.Modified = purchase.Modified; x.ModifiedBy = purchase.ModifiedBy; x.PurchaseStatusId = purchase.PurchaseStatusId; });

                foreach (var detail in purchase.Detail)
                {
                    detail.State = true;
                    if (detail.Id == Guid.Empty)
                    {
                        detail.PurchaseId = purchase.Id;
                        detail.CreatedBy = purchase.ModifiedBy;
                        detail.Created = purchase.Modified;
                        await _purchaseDetailRepository.CreatePurchaseDetail(detail, db, transaction);
                    }
                    else
                    {
                        await _purchaseDetailRepository.UpdatePurchaseDetail(detail, db, transaction);
                    }
                }
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<int>(ex); }
        finally { db.Close(); }

        return numberRows;
    }
    public async Task<int> ReceiveOrders(PurchaseDelivery purchaseDelivery)
    {
        int numberRows = 0;
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                purchaseDelivery.Id = Guid.NewGuid();
                string sqlQuery = @"INSERT INTO purchases_delivery
                                        (id, purchase_id, delivery_date, state, created_by, created,   modified_by, modified)
                                  VALUES(@Id, @PurchaseId, @DeliveryDate, @state, @CreatedBy, @Created, @ModifiedBy, @Modified); ";

                numberRows = await db.ExecuteAsync(sqlQuery, purchaseDelivery);
                foreach (var detail in purchaseDelivery.Detail)
                {
                    //if(detail.IsDelivered){
                    detail.PurchaseDeliveryId = purchaseDelivery.Id;
                    await _purchaseDetailRepository.ReceiveOrdersDetail(detail, db, transaction);
                    //}
                }
                transaction.Commit();
            }
            catch (CustomException ex)
            {
                transaction.Rollback();
                throw new CustomException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<int>(ex); }
        finally { db.Close(); }
        return numberRows;
    }

    public async Task<List<PurchaseProductResponse>> GetPurchases(DateTime purchaseDateInitial, DateTime purchaseDateEnd, Domain.Enums.PurchaseStatusEnum purchaseStatus)
    {
        List<PurchaseProductResponse> listPurchases = [];
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            string sqlQuery = @"
                        SELECT p.id, p.purchase_date, p.total, p.is_active, ps.description as PurchaseStatusName, p.purchase_status_id,
                            p.provider_id, pr.provider_name
							FROM purchases p 
                                 INNER JOIN purchases_status ps
                              ON ps.id = p.purchase_status_id
                                 INNER JOIN providers pr
                              ON pr.id = p.provider_id
							WHERE p.state 
							  AND p.is_active 
                              AND p.purchase_date >= @PurchaseDateInitial
                              AND p.purchase_date <= @PurchaseDateEnd
							  AND p.id IN (
							    SELECT pd.purchase_id 
							    FROM purchases_detail pd 
							    WHERE p.purchase_status_id = @PurchaseStatusId
                                  AND pd.state
							    GROUP BY pd.purchase_id 
								);
                ";

            var result = await db.QueryAsync<PurchaseProductResponse>(sqlQuery,
                new
                {
                    PurchaseDateInitial = purchaseDateInitial,
                    PurchaseDateEnd = purchaseDateEnd,
                    PurchaseStatusId = (int)purchaseStatus
                });
            listPurchases = result!.ToList();
            foreach (var item in listPurchases)
            {
                sqlQuery = @" SELECT pd.id,
                                        pd.purchase_id,
                                        pd.product_id,
                                        p.product_name,
                                        pd.ordered_quantity,
                                        pd.order_unit_price,
                                        pd.order_final_price,
                                        pd.purchase_status_id
                                   FROM purchases_detail pd
                                        INNER JOIN products p ON p.id = pd.product_id
                                  WHERE pd.purchase_id = @purchase_id
                                    AND pd.state";
                var resultDetail = db.Query<PurchaseProductDetailResponse>(sqlQuery, new { purchase_id = item.Id });
                item.Detail = resultDetail.ToList();
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }

        return listPurchases;
    }

    public async Task<List<Purchase>> GetPurchases(string PurchaseDate)
    {
        List<Purchase> listPurchases = [];
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            string sqlQuery = @"
                        SELECT id, purchase_date, total_price, is_active
                         FROM Purchases
                        WHERE state
                          AND purchase_date= @PurchaseDate;                ";

            var result = await db.QueryAsync<Purchase>(sqlQuery,
                new { PurchaseDate = PurchaseDate });
            listPurchases = result!.ToList();

        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }

        return listPurchases;
    }

    public async Task<PurchaseProductResponse> GetPurchase(Guid Id)
    {
        PurchaseProductResponse purchase = new();
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            string sqlQuery = @"
                        SELECT p.id, p.purchase_date, p.total, p.is_active, ps.description as PurchaseStatusName, p.purchase_status_id,
                            p.provider_id, pr.provider_name, p.estimated_delivery_date
                          FROM purchases p
                         INNER JOIN purchases_status ps ON ps.id = p.purchase_status_id
                         INNER JOIN providers pr        ON pr.id = p.provider_id
                         WHERE p.state
                           AND p.id = @Id;
                        ";

            purchase = await db.QueryFirstOrDefaultAsync<PurchaseProductResponse>(sqlQuery,
                new { Id }) ?? new PurchaseProductResponse();

            sqlQuery = @" SELECT pd.id,
                                        pd.purchase_id,
                                        pd.product_id,
                                        p.product_name,
                                        pd.ordered_quantity,
                                        pd.order_unit_price,
                                        pd.order_final_price,
                                        pd.purchase_status_id
                                   FROM purchases_detail pd
                                        INNER JOIN products p ON p.id = pd.product_id
                                  WHERE pd.purchase_id = @purchase_id
                                    AND pd.state";

            var resultDetail = db.Query<PurchaseProductDetailResponse>(sqlQuery, new { purchase_id = purchase.Id });
            purchase.Detail = resultDetail.ToList();


        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }

        return purchase;
    }

    public async Task<int> DeletePurchase(Guid id, int idUserModified)
    {
        using var db = _DbContext.CreateConnection;
        int numberRows = 0;
        try
        {
            DateTime fechaActual = DateTime.Now;
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                string sqlQuery = @"
                        UPDATE purchases
                           SET state = false,
                               modified_by = @ModifiedBy, 
                               modified = @Modified
                         WHERE id = @Id ;
                    ";
                numberRows = await db.ExecuteAsync(sqlQuery, new { Id = id, ModifiedBy = idUserModified, @Modified = fechaActual });
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }

        return numberRows;
    }

}
